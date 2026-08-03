using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GharCraft.Infrastructure.Persistence;

public class GharCraftDbContextFactory : IDesignTimeDbContextFactory<GharCraftDbContext>
{
    public GharCraftDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GharCraftDbContext>();
        
        var connectionString = "Host=localhost;Port=5432;Database=gharcraft;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(GharCraftDbContext).Assembly.FullName);
        });

        return new GharCraftDbContext(optionsBuilder.Options);
    }
}
