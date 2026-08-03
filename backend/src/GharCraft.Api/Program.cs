using GharCraft.Api.Extensions;
using GharCraft.Application;
using GharCraft.Infrastructure;
using GharCraft.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Dynamic PORT binding for Railway / Render
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks — PostgreSQL connectivity via DbContext
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GharCraftDbContext>("db");

var app = builder.Build();

// ── Health check endpoints (must be FIRST, before any auth middleware) ──────
// /healthz → fast liveness probe (no DB call) — used by Railway container
// /readyz  → readiness probe (checks DB connectivity)
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
app.MapHealthChecks("/readyz");

// ── Database migration & seeding ────────────────────────────────────────────
try
{
    await DataSeeder.SeedDatabaseAsync(app.Services, app.Configuration);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database seeding/migration failed on startup.");
    // Do NOT throw — allow app to stay up so /healthz still passes
}

// ── Swagger (Dev + Staging) ──────────────────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() ||
    app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GharCraft API v1");
        c.RoutePrefix = "swagger";
    });
}

// NOTE: No UseHttpsRedirection — Railway/Render terminate TLS at the load balancer.
//       Adding it would cause redirect loops and break health checks.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
