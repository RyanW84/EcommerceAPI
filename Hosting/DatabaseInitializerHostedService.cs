using ECommerceApp.RyanW84.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.RyanW84.Hosting;

/// <summary>
/// Applies migrations at startup. In Development, optionally resets and seeds the database.
/// Keeps Program.cs lean and satisfies SRP.
/// </summary>
public class DatabaseInitializerHostedService(IServiceProvider services,
    IWebHostEnvironment env,
    ILogger<DatabaseInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        try
        {
            if (env.IsDevelopment())
            {
                logger.LogInformation("Development mode: ensuring clean database before migration...");
                await db.Database.EnsureDeletedAsync(cancellationToken);
            }

            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied.");

            if (env.IsDevelopment())
            {
                db.SeedData();
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Database seeded with initial data.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
