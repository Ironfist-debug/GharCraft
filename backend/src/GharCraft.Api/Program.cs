using GharCraft.Api.Extensions;
using GharCraft.Application;
using GharCraft.Infrastructure;
using GharCraft.Infrastructure.Persistence;

using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Dynamic PORT binding for Railway / Render (Only override if PORT is provided)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

// Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── 1. Health check (FIRST — before everything else) ────────────────────────
// Simple 200 OK — no DB dependency so Railway liveness probe always passes.
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// ── 2. Database migration & seeding ─────────────────────────────────────────
try
{
    await DataSeeder.SeedDatabaseAsync(app.Services, app.Configuration);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database seeding/migration failed on startup — app will continue.");
}

// ── 3. Swagger (Dev + Staging) ───────────────────────────────────────────────
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

// NOTE: No UseHttpsRedirection — Railway terminates TLS at the load balancer.

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
