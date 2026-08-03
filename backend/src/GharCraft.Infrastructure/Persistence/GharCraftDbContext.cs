using System.Reflection;
using GharCraft.Domain.Common;
using GharCraft.Domain.Entities.Identity;
using GharCraft.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GharCraft.Infrastructure.Persistence;

public class GharCraftDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly AuditableEntityInterceptor? _auditableInterceptor;
    private readonly SoftDeleteInterceptor? _softDeleteInterceptor;

    public GharCraftDbContext(
        DbContextOptions<GharCraftDbContext> options,
        AuditableEntityInterceptor? auditableInterceptor = null,
        SoftDeleteInterceptor? softDeleteInterceptor = null)
        : base(options)
    {
        _auditableInterceptor = auditableInterceptor;
        _softDeleteInterceptor = softDeleteInterceptor;
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditableInterceptor is not null)
            optionsBuilder.AddInterceptors(_auditableInterceptor);

        if (_softDeleteInterceptor is not null)
            optionsBuilder.AddInterceptors(_softDeleteInterceptor);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply entity configurations from assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter for soft delete
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(GharCraftDbContext)
                    .GetMethod(nameof(SetSoftDeleteQueryFilter), BindingFlags.NonPublic | BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { builder });
            }
        }
    }

    private static void SetSoftDeleteQueryFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ISoftDeletable
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}
