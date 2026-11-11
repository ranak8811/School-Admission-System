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
        var defaultClasses = new[]
        {
            new Api.Models.Entities.ClassEntity { Id = "c_c6m", Name = "Class 6 - Morning Shift", TotalSeats = 120 },
            new Api.Models.Entities.ClassEntity { Id = "c_c6d", Name = "Class 6 - Day Shift", TotalSeats = 100 },
            new Api.Models.Entities.ClassEntity { Id = "c_c7m", Name = "Class 7 - Morning Shift", TotalSeats = 80 },
            new Api.Models.Entities.ClassEntity { Id = "c_c7d", Name = "Class 7 - Day Shift", TotalSeats = 80 },
            new Api.Models.Entities.ClassEntity { Id = "c_c8m", Name = "Class 8 - Morning Shift", TotalSeats = 75 },
            new Api.Models.Entities.ClassEntity { Id = "c_c8d", Name = "Class 8 - Day Shift", TotalSeats = 75 },
            new Api.Models.Entities.ClassEntity { Id = "c_c9m", Name = "Class 9 - Morning Shift", TotalSeats = 60 },
            new Api.Models.Entities.ClassEntity { Id = "c_c9d", Name = "Class 9 - Day Shift", TotalSeats = 60 },
            new Api.Models.Entities.ClassEntity { Id = "c_c10m", Name = "Class 10 - Morning Shift", TotalSeats = 50 }
        };
        await context.Classes.AddRangeAsync(defaultClasses);
        await context.SaveChangesAsync();

        // Assert
        var classes = await context.Classes.ToListAsync();
        Assert.Equal(9, classes.Count);
        Assert.Contains(classes, c => c.Id == "c_c6m" && c.TotalSeats == 120);
        Assert.Contains(classes, c => c.Id == "c_c8m" && c.TotalSeats == 75);
        Assert.Contains(classes, c => c.Id == "c_c9m" && c.TotalSeats == 60);
    }
}
