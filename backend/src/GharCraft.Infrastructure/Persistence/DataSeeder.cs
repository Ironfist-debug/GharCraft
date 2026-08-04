using GharCraft.Domain.Entities.Identity;
using GharCraft.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GharCraft.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GharCraftDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GharCraftDbContext>>();

        // 1. Auto-apply pending EF Core migrations
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            logger.LogInformation("Applying pending EF Core migrations to PostgreSQL database...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }

        // 2. Ensure default roles exist (Admin, Customer)
        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                logger.LogInformation("Seeded role: {Role}", roleName);
            }
        }

        // 3. Seed initial Admin user from configuration (required — no fallback password)
        var adminEmail = configuration["AdminSeed:Email"] ?? "admin@gharcraft.com";
        var adminPassword = configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "AdminSeed:Password is not configured — skipping admin user seed. " +
                "Set AdminSeed:Password in appsettings.Local.json or as an environment variable.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "GharCraft",
                LastName = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                logger.LogInformation("Initial Admin user created successfully: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to create seed Admin user: {Errors}", errors);
            }
        }
    }
}
