using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Api.Controllers;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using Xunit;

namespace SchoolAdmission.Tests.Controllers;

public class HoldsControllerTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private HoldsController CreateController(AppDbContext context)
    {
        return new HoldsController(context, NullLogger<HoldsController>.Instance);
    }

    [Fact]
    public async Task GetHoldById_WhenHoldIsActiveAndUnexpired_Returns200WithHeldStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_h1", Name = "Class 6 Morning", TotalSeats = 50 };
        context.Classes.Add(testClass);

        var futureExpiry = DateTime.UtcNow.AddMinutes(8);
        var activeHold = new SeatHoldEntity
        {
            Id = "h_active",
            ClassId = "c_h1",
            StudentId = "20152691234567890",
            Status = HoldStatus.HELD,
            ExpiresAt = futureExpiry
        };
        context.SeatHolds.Add(activeHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHoldById("h_active", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var response = Assert.IsType<HoldStatusResponse>(okResult.Value);
        Assert.Equal("h_active", response.HoldId);
        Assert.Equal("c_h1", response.ClassId);
        Assert.Equal("HELD", response.Status);
        Assert.Equal(DateTimeKind.Utc, response.ExpiresAt.Kind);
    }

    [Fact]
    public async Task GetHoldById_WhenHoldExpiresAtIsInPast_Returns200WithExpiredStatusDynamically()
    {
        // Arrange: Hold is stored as HELD in DB, but expiresAt is in the past
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_h2", Name = "Class 7 Day", TotalSeats = 50 };
        context.Classes.Add(testClass);

        var pastExpiry = DateTime.UtcNow.AddMinutes(-3);
        var expiredHold = new SeatHoldEntity
        {
            Id = "h_expired",
            ClassId = "c_h2",
            StudentId = "20152691234567891",
            Status = HoldStatus.HELD, // DB record not yet swept
            ExpiresAt = pastExpiry
        };
        context.SeatHolds.Add(expiredHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHoldById("h_expired", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var response = Assert.IsType<HoldStatusResponse>(okResult.Value);
        Assert.Equal("h_expired", response.HoldId);
        Assert.Equal("c_h2", response.ClassId);
        Assert.Equal("EXPIRED", response.Status); // Dynamically calculated
    }

    [Fact]
    public async Task GetHoldById_WhenHoldIsConfirmed_Returns200WithConfirmedStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_h3", Name = "Class 8 Morning", TotalSeats = 50 };
        context.Classes.Add(testClass);

        var confirmedHold = new SeatHoldEntity
        {
            Id = "h_confirmed",
            ClassId = "c_h3",
            StudentId = "20152691234567892",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-20)
        };
        context.SeatHolds.Add(confirmedHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHoldById("h_confirmed", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var response = Assert.IsType<HoldStatusResponse>(okResult.Value);
        Assert.Equal("h_confirmed", response.HoldId);
        Assert.Equal("CONFIRMED", response.Status);
    }

    [Fact]
    public async Task GetHoldById_WhenHoldIsCancelled_Returns200WithCancelledStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_h4", Name = "Class 9 Day", TotalSeats = 50 };
        context.Classes.Add(testClass);

        var cancelledHold = new SeatHoldEntity
        {
            Id = "h_cancelled",
            ClassId = "c_h4",
            StudentId = "20152691234567893",
            Status = HoldStatus.CANCELLED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        context.SeatHolds.Add(cancelledHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHoldById("h_cancelled", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var response = Assert.IsType<HoldStatusResponse>(okResult.Value);
        Assert.Equal("h_cancelled", response.HoldId);
        Assert.Equal("CANCELLED", response.Status);
    }

    [Fact]
    public async Task GetHoldById_WhenHoldDoesNotExist_Returns404NotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.GetHoldById("h_nonexistent", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal("Not Found", problemDetails.Title);
        Assert.Contains("does not exist", problemDetails.Detail);
    }
}
