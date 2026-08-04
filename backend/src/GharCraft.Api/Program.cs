using System.Threading.RateLimiting;

using GharCraft.Api.Extensions;
using GharCraft.Api.Middleware;
using GharCraft.Application;
using GharCraft.Infrastructure;
using GharCraft.Infrastructure.Persistence;

using FluentValidation.AspNetCore;

using Serilog;
using Serilog.Events;

// ── Serilog bootstrap logger (captures startup errors before host is built) ───
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (replaces default Microsoft logging) ──────────────────────────
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            "logs/gharcraft-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    // Dynamic PORT binding for Railway / Render
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        builder.WebHost.UseUrls($"http://+:{port}");
    }

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ── Rate limiting (fixed-window per IP) ───────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Auth endpoints get a much tighter limit
        options.AddPolicy("auth", ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter = "60";
            await ctx.HttpContext.Response.WriteAsJsonAsync(new
            {
                status = 429,
                title = "Too many requests. Please slow down.",
                retryAfter = 60
            }, ct);
        };
    });

    var app = builder.Build();

    // ── Startup guard — fail fast on missing required config ──────────────────
    if (!app.Environment.IsDevelopment())
    {
        var jwtSecret = app.Configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 characters. " +
                "Set it via the Jwt__Secret environment variable on the hosting platform.");
        }
    }

    // ── 1. Global exception handler (must be first) ───────────────────────────
    app.UseExceptionHandler();

    // ── 2. Security headers ───────────────────────────────────────────────────
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["X-XSS-Protection"] = "0"; // modern browsers rely on CSP, not this
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        await next();
    });

    // ── 3. Correlation ID ─────────────────────────────────────────────────────
    app.Use(async (ctx, next) =>
    {
        var correlationId = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12];
        ctx.Response.Headers["X-Correlation-ID"] = correlationId;
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next();
        }
    });

    // ── 4. Serilog request logging ────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    });

    // ── 5. Health check (before rate limiter — no auth, no rate limit) ────────
    app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

    // ── 6. Database migration & seeding ──────────────────────────────────────
    try
    {
        await DataSeeder.SeedDatabaseAsync(app.Services, app.Configuration);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database seeding/migration failed on startup — app will continue.");
    }

    // ── 7. Swagger (Development only) ────────────────────────────────────────
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "GharCraft API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // NOTE: No UseHttpsRedirection — Railway terminates TLS at the load balancer.

    // ── 8. Middleware pipeline ────────────────────────────────────────────────
    app.UseRateLimiter();
    app.UseCors("GharCraftPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
