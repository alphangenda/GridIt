using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Persistence;

public class GarneauTemplateDbContextFactory : IDesignTimeDbContextFactory<GarneauTemplateDbContext>
{
    public GarneauTemplateDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=garneau-template;Username=postgres;Password=postgres";

        var dataSource = new NpgsqlDataSourceBuilder(connectionString)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<GarneauTemplateDbContext>();
        optionsBuilder
            .UseNpgsql(
                dataSource,
                opt => opt
                    .MigrationsAssembly(typeof(GarneauTemplateDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new GarneauTemplateDbContext(optionsBuilder.Options);
    }
}
