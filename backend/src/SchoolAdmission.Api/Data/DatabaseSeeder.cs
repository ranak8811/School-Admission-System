using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Data;

/// <summary>
/// Handles automatic database migration application and development seed data initialization.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            if (dbContext.Database.IsRelational())
            {
                logger.LogInformation("Applying pending database migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Seed initial classes if the catalog is empty
            if (!await dbContext.Classes.AnyAsync())
            {
                logger.LogInformation("Seeding initial class catalog...");
                var initialClasses = new List<ClassEntity>
                {
                    new()
                    {
                        Id = "c_c6m",
                        Name = "Class 6 - Morning Shift",
                        TotalSeats = 120,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = "c_c6d",
                        Name = "Class 6 - Day Shift",
                        TotalSeats = 100,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = "c_c7m",
                        Name = "Class 7 - Morning Shift",
                        TotalSeats = 80,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await dbContext.Classes.AddRangeAsync(initialClasses);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Initial class catalog seeded with 3 shift offerings.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }
    }
}
