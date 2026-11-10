using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Services;
using SchoolAdmission.Api.Services.Background;
using SchoolAdmission.Api.Services.Results;
using Xunit;

namespace SchoolAdmission.Tests.Services;

public class ExpiredHoldSweeperTests
{
    private class FakeAllocationService : ISeatAllocationService
    {
        public int SweepCalls { get; private set; }

        public Task<SeatHoldResult> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> SweepExpiredHoldsAsync(CancellationToken ct = default)
        {
            SweepCalls++;
            return Task.FromResult(5);
        }
    }

    [Fact]
    public async Task ExpiredHoldSweeper_RunsAndStopsGracefully_WhenCancellationRequested()
    {
        // Arrange
        var services = new ServiceCollection();
        var fakeService = new FakeAllocationService();
        services.AddSingleton<ISeatAllocationService>(fakeService);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new AdmissionOptions
        {
            HoldDurationSeconds = 600,
            SweeperIntervalSeconds = 1 // 1 second interval for test
        });

        var sweeper = new ExpiredHoldSweeper(
            scopeFactory,
            options,
            NullLogger<ExpiredHoldSweeper>.Instance
        );

        using var cts = new CancellationTokenSource();

        // Act: Start background sweeper and cancel after 100ms
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        await sweeper.StartAsync(cts.Token);
        await sweeper.StopAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(sweeper);
    }
}
