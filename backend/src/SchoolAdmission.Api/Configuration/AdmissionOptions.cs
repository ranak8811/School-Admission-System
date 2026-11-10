namespace SchoolAdmission.Api.Configuration;

/// <summary>
/// Configuration options for seat hold reservation lifecycle and expiration sweeper.
/// Can be configured via appsettings.json or environment variables (e.g. HOLD_DURATION_SECONDS).
/// </summary>
public class AdmissionOptions
{
    public const string SectionName = "AdmissionSettings";

    /// <summary>
    /// Duration in seconds a seat remains reserved on HELD status.
    /// Default is 600 seconds (10 minutes).
    /// </summary>
    public int HoldDurationSeconds { get; set; } = 600;

    /// <summary>
    /// Polling interval in seconds for the background expired hold sweeper worker.
    /// Default is 30 seconds.
    /// </summary>
    public int SweeperIntervalSeconds { get; set; } = 30;
}
