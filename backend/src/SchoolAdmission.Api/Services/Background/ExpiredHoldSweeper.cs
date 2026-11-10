using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Configuration;

namespace SchoolAdmission.Api.Services.Background;

/// <summary>
/// Background worker service that periodically sweeps and transitions expired seat holds
/// from HELD to EXPIRED status in the database.
/// Runs automatically alongside the ASP.NET Core web server.
/// </summary>
public class ExpiredHoldSweeper : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AdmissionOptions _options;
    private readonly ILogger<ExpiredHoldSweeper> _logger;

    public ExpiredHoldSweeper(
        IServiceScopeFactory scopeFactory,
        IOptions<AdmissionOptions> options,
        ILogger<ExpiredHoldSweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _options.SweeperIntervalSeconds > 0 ? _options.SweeperIntervalSeconds : 30;
        _logger.LogInformation("ExpiredHoldSweeper started. Sweep interval: {Interval} seconds", intervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var allocationService = scope.ServiceProvider.GetRequiredService<ISeatAllocationService>();

                var sweptCount = await allocationService.SweepExpiredHoldsAsync(stoppingToken);
                if (sweptCount > 0)
                {
                    _logger.LogInformation("ExpiredHoldSweeper completed sweep cycle: {Count} holds transitioned to EXPIRED", sweptCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal application shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during ExpiredHoldSweeper execution cycle");
            }
        }

        _logger.LogInformation("ExpiredHoldSweeper background service is stopping");
    }
}
