using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Api.Data;
using Xunit;

namespace SchoolAdmission.Tests.Data;

public class DatabaseSeederTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_SeedsDefaultClasses()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var logger = NullLogger.Instance;

        // Act: Seed when empty
        if (!await context.Classes.AnyAsync())
        {
            var initialClasses = new[]
            {
                new Api.Models.Entities.ClassEntity { Id = "c_c6m", Name = "Class 6 - Morning Shift", TotalSeats = 120 },
                new Api.Models.Entities.ClassEntity { Id = "c_c6d", Name = "Class 6 - Day Shift", TotalSeats = 100 },
                new Api.Models.Entities.ClassEntity { Id = "c_c7m", Name = "Class 7 - Morning Shift", TotalSeats = 80 }
            };
            await context.Classes.AddRangeAsync(initialClasses);
            await context.SaveChangesAsync();
        }

        // Assert
        var classes = await context.Classes.ToListAsync();
        Assert.Equal(3, classes.Count);
        Assert.Contains(classes, c => c.Id == "c_c6m" && c.TotalSeats == 120);
    }
}
