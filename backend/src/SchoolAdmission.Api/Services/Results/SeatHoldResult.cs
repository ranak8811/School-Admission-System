using Microsoft.AspNetCore.Http;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Services.Results;

/// <summary>
/// Domain operation outcome for seat hold reservation workflows.
/// </summary>
public record SeatHoldResult
{
    public bool IsSuccess { get; init; }
    public SeatHoldEntity? Hold { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static SeatHoldResult Success(SeatHoldEntity hold) =>
        new()
        {
            IsSuccess = true,
            Hold = hold,
            StatusCode = StatusCodes.Status201Created
        };

    public static SeatHoldResult Conflict(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status409Conflict
        };

    public static SeatHoldResult NotFound(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status404NotFound
        };

    public static SeatHoldResult BadRequest(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status400BadRequest
        };
}
