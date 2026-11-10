using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Controllers;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs.Holds;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;
using SchoolAdmission.Api.Services;
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

    private HoldsController CreateController(AppDbContext context, int holdDurationSeconds = 600)
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

        return new HoldsController(
            context,
            allocationService,
            NullLogger<HoldsController>.Instance
        );
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

    [Fact]
    public async Task ConfirmHold_WithMatchingStudentId_Returns200OK_AndSetsStatusConfirmed()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_conf1", Name = "Class 6 Morning", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567890";
        var hold = new SeatHoldEntity
        {
            Id = "h_conf1",
            ClassId = "c_conf1",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(9)
        };
        context.SeatHolds.Add(hold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.ConfirmHold("h_conf1", studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var response = Assert.IsType<ConfirmHoldResponse>(okResult.Value);
        Assert.Equal("h_conf1", response.HoldId);
        Assert.Equal("CONFIRMED", response.Status);

        // Verify entity persisted in DB
        var savedHold = await context.SeatHolds.FindAsync("h_conf1");
        Assert.Equal(HoldStatus.CONFIRMED, savedHold!.Status);
    }

    [Fact]
    public async Task ConfirmHold_WhenCalledMultipleTimes_IsIdempotent_Returns200OK_WithoutDoubleConfirming()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_conf2", Name = "Class 6 Day", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567891";
        var hold = new SeatHoldEntity
        {
            Id = "h_conf2",
            ClassId = "c_conf2",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(9)
        };
        context.SeatHolds.Add(hold);
        await context.SaveChangesAsync();

        // Act: Call confirmation twice consecutively
        var firstCall = await controller.ConfirmHold("h_conf2", studentId, CancellationToken.None);
        var secondCall = await controller.ConfirmHold("h_conf2", studentId, CancellationToken.None);

        // Assert: Both return HTTP 200 OK with CONFIRMED
        var firstOk = Assert.IsType<OkObjectResult>(firstCall);
        var secondOk = Assert.IsType<OkObjectResult>(secondCall);

        Assert.Equal(200, firstOk.StatusCode);
        Assert.Equal(200, secondOk.StatusCode);

        var firstResp = Assert.IsType<ConfirmHoldResponse>(firstOk.Value);
        var secondResp = Assert.IsType<ConfirmHoldResponse>(secondOk.Value);

        Assert.Equal("CONFIRMED", firstResp.Status);
        Assert.Equal("CONFIRMED", secondResp.Status);

        // Check total confirmed count is exactly 1
        var confirmedCount = await context.SeatHolds.CountAsync(h => h.ClassId == "c_conf2" && h.Status == HoldStatus.CONFIRMED);
        Assert.Equal(1, confirmedCount);
    }

    [Fact]
    public async Task ConfirmHold_WhenHoldIsExpired_Returns409Conflict()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_conf3", Name = "Class 7", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567892";
        var expiredHold = new SeatHoldEntity
        {
            Id = "h_conf_expired",
            ClassId = "c_conf3",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired in past
        };
        context.SeatHolds.Add(expiredHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.ConfirmHold("h_conf_expired", studentId, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal("Conflict", problemDetails.Title);
        Assert.Contains("expired or has already been cancelled", problemDetails.Detail);
    }

    [Fact]
    public async Task ConfirmHold_WhenHoldIsCancelled_Returns409Conflict()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_conf4", Name = "Class 8", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567893";
        var cancelledHold = new SeatHoldEntity
        {
            Id = "h_conf_cancelled",
            ClassId = "c_conf4",
            StudentId = studentId,
            Status = HoldStatus.CANCELLED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        context.SeatHolds.Add(cancelledHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.ConfirmHold("h_conf_cancelled", studentId, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task ConfirmHold_WhenStudentIdDoesNotMatch_Returns403Forbidden()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_conf5", Name = "Class 9", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var ownerStudentId = "20152691234567894";
        var imposterStudentId = "20152691234567899";

        var hold = new SeatHoldEntity
        {
            Id = "h_conf5",
            ClassId = "c_conf5",
            StudentId = ownerStudentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(8)
        };
        context.SeatHolds.Add(hold);
        await context.SaveChangesAsync();

        // Act: Imposter tries to confirm someone else's hold
        var result = await controller.ConfirmHold("h_conf5", imposterStudentId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Forbidden", problemDetails.Title);
        Assert.Contains("does not match hold record", problemDetails.Detail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456")]
    [InlineData("2015269123456789")]
    [InlineData("201526912345678901")]
    [InlineData("2015269123456789a")]
    public async Task ConfirmHold_WithMissingOrInvalidStudentIdHeader_Returns400BadRequest(string? studentId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.ConfirmHold("h_any", studentId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Header X-Student-Id is missing or must be 17 digits", problemDetails.Detail);
    }

    [Fact]
    public async Task ConfirmHold_WhenHoldDoesNotExist_Returns404NotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.ConfirmHold("h_missing", "20152691234567890", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains("does not exist", problemDetails.Detail);
    }

    [Fact]
    public async Task CancelHold_WithMatchingStudentId_Returns204NoContent_AndSetsStatusCancelled()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_canc1", Name = "Class 6 Morning", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567890";
        var hold = new SeatHoldEntity
        {
            Id = "h_canc1",
            ClassId = "c_canc1",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(9)
        };
        context.SeatHolds.Add(hold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.CancelHold("h_canc1", studentId, CancellationToken.None);

        // Assert: 204 NoContent with empty body
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        // Verify entity persisted as CANCELLED in DB
        var savedHold = await context.SeatHolds.FindAsync("h_canc1");
        Assert.Equal(HoldStatus.CANCELLED, savedHold!.Status);
    }

    [Fact]
    public async Task CancelHold_WhenHoldIsConfirmed_Returns400BadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_canc2", Name = "Class 6 Day", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567891";
        var confirmedHold = new SeatHoldEntity
        {
            Id = "h_canc_conf",
            ClassId = "c_canc2",
            StudentId = studentId,
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.SeatHolds.Add(confirmedHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.CancelHold("h_canc_conf", studentId, CancellationToken.None);

        // Assert: 400 Bad Request
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Cannot cancel a confirmed admission", problemDetails.Detail);
    }

    [Fact]
    public async Task CancelHold_WhenHoldIsExpired_Returns409Conflict()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_canc3", Name = "Class 7", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567892";
        var expiredHold = new SeatHoldEntity
        {
            Id = "h_canc_exp",
            ClassId = "c_canc3",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        context.SeatHolds.Add(expiredHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.CancelHold("h_canc_exp", studentId, CancellationToken.None);

        // Assert: 409 Conflict
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Contains("expired", problemDetails.Detail);
    }

    [Fact]
    public async Task CancelHold_WhenStudentIdDoesNotMatch_Returns403Forbidden()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_canc4", Name = "Class 8", TotalSeats = 20 };
        context.Classes.Add(testClass);

        var ownerStudentId = "20152691234567893";
        var imposterStudentId = "20152691234567899";

        var hold = new SeatHoldEntity
        {
            Id = "h_canc4",
            ClassId = "c_canc4",
            StudentId = ownerStudentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(9)
        };
        context.SeatHolds.Add(hold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.CancelHold("h_canc4", imposterStudentId, CancellationToken.None);

        // Assert: 403 Forbidden
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("does not match hold record", problemDetails.Detail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456")]
    [InlineData("2015269123456789")]
    [InlineData("201526912345678901")]
    [InlineData("2015269123456789a")]
    public async Task CancelHold_WithMissingOrInvalidStudentIdHeader_Returns400BadRequest(string? studentId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.CancelHold("h_any", studentId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Header X-Student-Id is missing or must be 17 digits", problemDetails.Detail);
    }

    [Fact]
    public async Task CancelHold_WhenHoldDoesNotExist_Returns404NotFound()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.CancelHold("h_missing", "20152691234567890", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Contains("does not exist", problemDetails.Detail);
    }
}
