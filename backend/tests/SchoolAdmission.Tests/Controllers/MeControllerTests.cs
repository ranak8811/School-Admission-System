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

public class MeControllerTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private MeController CreateController(AppDbContext context)
    {
        return new MeController(context, NullLogger<MeController>.Instance);
    }

    [Fact]
    public async Task GetMyHolds_WithValidStudentId_ReturnsAllHoldsMatchingStudent_WithClassNameAndContract()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var class1 = new ClassEntity { Id = "c_me1", Name = "Class 6 Morning", TotalSeats = 50 };
        var class2 = new ClassEntity { Id = "c_me2", Name = "Class 7 Day", TotalSeats = 40 };
        context.Classes.AddRange(class1, class2);

        var studentA = "20152691234567890";
        var studentB = "20152691234567899";

        // Student A: Hold in Class 1 (active)
        var holdA1 = new SeatHoldEntity
        {
            Id = "h_a1",
            ClassId = "c_me1",
            StudentId = studentA,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(8),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Student A: Hold in Class 2 (confirmed)
        var holdA2 = new SeatHoldEntity
        {
            Id = "h_a2",
            ClassId = "c_me2",
            StudentId = studentA,
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow.AddMinutes(-35)
        };

        // Student B: Hold in Class 1 (different student)
        var holdB1 = new SeatHoldEntity
        {
            Id = "h_b1",
            ClassId = "c_me1",
            StudentId = studentB,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        context.SeatHolds.AddRange(holdA1, holdA2, holdB1);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetMyHolds(studentA, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var items = Assert.IsAssignableFrom<IEnumerable<StudentHoldItemResponse>>(okResult.Value);
        var list = items.ToList();

        // Should return exactly 2 items for student A, ordered by CreatedAt desc
        Assert.Equal(2, list.Count);

        var first = list[0];
        Assert.Equal("h_a1", first.HoldId);
        Assert.Equal("c_me1", first.ClassId);
        Assert.Equal("Class 6 Morning", first.ClassName);
        Assert.Equal("HELD", first.Status);
        Assert.Equal(DateTimeKind.Utc, first.ExpiresAt.Kind);

        var second = list[1];
        Assert.Equal("h_a2", second.HoldId);
        Assert.Equal("c_me2", second.ClassId);
        Assert.Equal("Class 7 Day", second.ClassName);
        Assert.Equal("CONFIRMED", second.Status);
    }

    [Fact]
    public async Task GetMyHolds_DynamicallyReportsExpired_WhenHoldExpiredInPast()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var testClass = new ClassEntity { Id = "c_me3", Name = "Class 8 Morning", TotalSeats = 50 };
        context.Classes.Add(testClass);

        var studentId = "20152691234567891";

        // Hold in DB is HELD, but expiresAt is in the past
        var staleHold = new SeatHoldEntity
        {
            Id = "h_stale",
            ClassId = "c_me3",
            StudentId = studentId,
            Status = HoldStatus.HELD,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // In past
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        context.SeatHolds.Add(staleHold);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetMyHolds(studentId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<StudentHoldItemResponse>>(okResult.Value);
        var item = Assert.Single(items);

        Assert.Equal("h_stale", item.HoldId);
        Assert.Equal("EXPIRED", item.Status); // Dynamically calculated
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456")]
    [InlineData("2015269123456789")]
    [InlineData("201526912345678901")]
    [InlineData("2015269123456789a")]
    public async Task GetMyHolds_WithMissingOrInvalidHeader_Returns400BadRequest(string? studentId)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.GetMyHolds(studentId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("Header X-Student-Id is missing or must be 17 digits", problemDetails.Detail);
    }
}
