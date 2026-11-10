using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using Xunit;

namespace SchoolAdmission.Tests.Data;

public class EntityModelMappingTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void ClassEntity_And_SeatHoldEntity_NavigationProperties_WorkCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var classEntity = new ClassEntity
        {
            Id = "c_101",
            Name = "Class 6 - Morning Shift",
            TotalSeats = 120
        };

        var holdEntity = new SeatHoldEntity
        {
            Id = "h_501",
            ClassId = "c_101",
            StudentId = "20152691234567890",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        classEntity.Holds.Add(holdEntity);
        context.Classes.Add(classEntity);
        context.SaveChanges();

        // Act
        var savedClass = context.Classes
            .Include(c => c.Holds)
            .FirstOrDefault(c => c.Id == "c_101");

        // Assert
        Assert.NotNull(savedClass);
        Assert.Equal("Class 6 - Morning Shift", savedClass.Name);
        Assert.Single(savedClass.Holds);
        Assert.Equal("20152691234567890", savedClass.Holds.First().StudentId);
        Assert.Equal(HoldStatus.HELD, savedClass.Holds.First().Status);
    }
}
