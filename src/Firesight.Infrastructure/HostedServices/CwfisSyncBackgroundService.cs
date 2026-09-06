using Firesight.Application.Wildfires;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Firesight.Infrastructure.HostedServices;

public sealed class CwfisSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CwfisSyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IWildfireService>();
            var result = await service.RefreshAsync(cancellationToken);

            logger.LogInformation(
                "CWFIS wildfire sync completed. Received {Received}, accepted {Accepted}, rejected {Rejected}, inserted {Inserted}, changed {Changed}, observed unchanged {Observed}.",
                result.Received,
                result.Accepted,
                result.Rejected,
                result.Inserted,
                result.Changed,
                result.Observed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "CWFIS wildfire sync failed. Existing data will remain available.");
        }
    }
}
