using Microsoft.AspNetCore.Http;

namespace SchoolAdmission.Api.Services.Results;

/// <summary>
/// Domain operation outcome for hold cancellation workflows.
/// </summary>
public record CancelHoldResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static CancelHoldResult Success() =>
        new()
        {
            IsSuccess = true,
            StatusCode = StatusCodes.Status204NoContent
        };

    public static CancelHoldResult Conflict(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status409Conflict
        };

    public static CancelHoldResult NotFound(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status404NotFound
        };

    public static CancelHoldResult Forbidden(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status403Forbidden
        };

    public static CancelHoldResult BadRequest(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message,
            StatusCode = StatusCodes.Status400BadRequest
        };
}
