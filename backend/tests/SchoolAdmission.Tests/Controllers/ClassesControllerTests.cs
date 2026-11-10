using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Controllers;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Classes;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using SchoolAdmission.Api.Services;
using Xunit;

namespace SchoolAdmission.Tests.Controllers;

public class ClassesControllerTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private ClassesController CreateController(AppDbContext context, int holdDurationSeconds = 600)
    {
        var options = Options.Create(new AdmissionOptions
        {
            HoldDurationSeconds = holdDurationSeconds,
            SweeperIntervalSeconds = 30
        });

        var allocationService = new SeatAllocationService(
            context,
            options,
            NullLogger<SeatAllocationService>.Instance
        );

        return new ClassesController(
            context,
            allocationService,
            NullLogger<ClassesController>.Instance
        );
    }

    [Fact]
    public async Task CreateClass_WithValidPayload_Returns201Created_AndMatchesContract()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);
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
        var controller = CreateController(context);
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
        var controller = CreateController(context);

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
        var controller = CreateController(context);

        // Act
        var result = await controller.GetClassById("c_nonexistent", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task CreateHold_WithValid17DigitStudentId_Returns201Created_AndMatchesContract()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context, holdDurationSeconds: 600);

        var testClass = new ClassEntity
        {
            Id = "c_hold_test",
            Name = "Class 6 Morning",
            TotalSeats = 20
        };
        context.Classes.Add(testClass);
        await context.SaveChangesAsync();

        var studentId = "20152691234567890";

        // Act
        var result = await controller.CreateHold("c_hold_test", studentId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        var response = Assert.IsType<CreateHoldResponse>(objectResult.Value);
        Assert.StartsWith("h_", response.HoldId);
        Assert.Equal("c_hold_test", response.ClassId);
        Assert.Equal(studentId, response.StudentId);
        Assert.Equal("HELD", response.Status);
        Assert.True(response.ExpiresAt > DateTime.UtcNow.AddMinutes(9));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456")]                    // 6 digits (too short)
    [InlineData("2015269123456789")]          // 16 digits (1 short)
    [InlineData("201526912345678901")]        // 18 digits (1 too long)
    [InlineData("2015269123456789a")]        // Contains alphanumeric letter
    public async Task CreateHold_WithMissingOrInvalidHeader_Returns400BadRequest(string? studentId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.CreateHold("c_any", studentId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Header X-Student-Id is missing or must be 17 digits", problemDetails.Detail);
    }

    [Fact]
    public async Task CreateHold_WhenClassIsSoldOut_Returns409Conflict()
    {
        // Arrange: Class with 1 seat, already held
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity
        {
            Id = "c_sold_out",
            Name = "Class Full",
            TotalSeats = 1
        };
        context.Classes.Add(testClass);

        var existingHold = new SeatHoldEntity
        {
            Id = "h_full_1",
            ClassId = "c_sold_out",
            StudentId = "20152691234567801",
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        context.SeatHolds.Add(existingHold);
        await context.SaveChangesAsync();

        // Act: Another student attempts to hold
        var result = await controller.CreateHold("c_sold_out", "20152691234567802", CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Contains("No seats available", problemDetails.Detail);
    }

    [Fact]
    public async Task CreateHold_WhenClassDoesNotExist_Returns404NotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.CreateHold("c_missing_class", "20152691234567899", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains("does not exist", problemDetails.Detail);
    }
}
