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

            // Seed initial classes or missing catalog offerings (Total 9 offerings for Class 6 to 10 Morning/Day shifts)
            var defaultClasses = new List<ClassEntity>
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
                },
                new()
                {
                    Id = "c_c7d",
                    Name = "Class 7 - Day Shift",
                    TotalSeats = 80,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "c_c8m",
                    Name = "Class 8 - Morning Shift",
                    TotalSeats = 75,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "c_c8d",
                    Name = "Class 8 - Day Shift",
                    TotalSeats = 75,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "c_c9m",
                    Name = "Class 9 - Morning Shift",
                    TotalSeats = 60,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "c_c9d",
                    Name = "Class 9 - Day Shift",
                    TotalSeats = 60,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "c_c10m",
                    Name = "Class 10 - Morning Shift",
                    TotalSeats = 50,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var existingIds = await dbContext.Classes.Select(c => c.Id).ToListAsync();
            var missingClasses = defaultClasses.Where(c => !existingIds.Contains(c.Id)).ToList();
            if (missingClasses.Count > 0)
            {
                logger.LogInformation("Seeding {Count} missing class catalog offerings...", missingClasses.Count);
                await dbContext.Classes.AddRangeAsync(missingClasses);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Class catalog successfully seeded with total standard offerings.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }
    }
}
