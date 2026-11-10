using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Api.Controllers;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Classes;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using Xunit;

namespace SchoolAdmission.Tests.Controllers;

public class ClassesControllerTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateClass_WithValidPayload_Returns201Created_AndMatchesContract()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = new ClassesController(context, NullLogger<ClassesController>.Instance);
        var request = new CreateClassRequest("Class 6 - Morning Shift", 120);

        // Act
        var result = await controller.CreateClass(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        var response = Assert.IsType<CreateClassResponse>(objectResult.Value);
        Assert.StartsWith("c_", response.Id);
        Assert.Equal("Class 6 - Morning Shift", response.Name);
        Assert.Equal(120, response.TotalSeats);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("   ", 100)]
    [InlineData("Class 10", 0)]
    [InlineData("Class 10", -5)]
    public async Task CreateClass_WithInvalidPayload_Returns400BadRequest(string name, int totalSeats)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = new ClassesController(context, NullLogger<ClassesController>.Instance);
        var request = new CreateClassRequest(name, totalSeats);

        // Act
        var result = await controller.CreateClass(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetClasses_CalculatesActiveHeldAndConfirmedSeatsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = new ClassesController(context, NullLogger<ClassesController>.Instance);

        var testClass = new ClassEntity
        {
            Id = "c_test1",
            Name = "Class 6 - Morning Shift",
            TotalSeats = 100
        };

        // Add 1 active hold
        testClass.Holds.Add(new SeatHoldEntity
        {
            Id = "h_1",
            ClassId = "c_test1",
            StudentId = "20152691234567891",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });

        // Add 1 expired hold (should NOT be counted in heldSeats)
        testClass.Holds.Add(new SeatHoldEntity
        {
            Id = "h_2",
            ClassId = "c_test1",
            StudentId = "20152691234567892",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired in past
        });

        // Add 2 confirmed seats
        testClass.Holds.Add(new SeatHoldEntity
        {
            Id = "h_3",
            ClassId = "c_test1",
            StudentId = "20152691234567893",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        });
        testClass.Holds.Add(new SeatHoldEntity
        {
            Id = "h_4",
            ClassId = "c_test1",
            StudentId = "20152691234567894",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-15)
        });

        context.Classes.Add(testClass);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetClasses(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var classes = Assert.IsAssignableFrom<IEnumerable<ClassDetailResponse>>(okResult.Value);
        var detail = Assert.Single(classes);

        Assert.Equal("c_test1", detail.Id);
        Assert.Equal(100, detail.TotalSeats);
        Assert.Equal(1, detail.HeldSeats);       // Only 1 unexpired hold
        Assert.Equal(2, detail.ConfirmedSeats);  // 2 confirmed
        Assert.Equal(97, detail.AvailableSeats); // 100 - (1 + 2) = 97
    }

    [Fact]
    public async Task GetClassById_WhenClassDoesNotExist_Returns404NotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = new ClassesController(context, NullLogger<ClassesController>.Instance);

        // Act
        var result = await controller.GetClassById("c_nonexistent", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
}
