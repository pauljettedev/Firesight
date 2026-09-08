using Firesight.Application.Common;

namespace Firesight.Application.Wildfires;

public sealed class WildfireService(
    IWildfireSource source,
    IWildfireRepository repository,
    IWildfireSyncStateRepository syncStateRepository) : IWildfireService
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public Task<IReadOnlyList<WildfireDto>> GetActiveWildfiresAsync(
        CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<WildfireDto?> GetWildfireByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default) =>
        repository.GetByExternalIdAsync(externalId, cancellationToken);

    public Task<IReadOnlyList<NearbyWildfireDto>> FindWildfiresNearAsync(
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            errors[nameof(latitude)] =
                ["Latitude must be a finite value between -90 and 90."];
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            errors[nameof(longitude)] =
                ["Longitude must be a finite value between -180 and 180."];
        }

        if (!double.IsFinite(radiusKm) || radiusKm <= 0)
        {
            errors[nameof(radiusKm)] =
                ["Radius must be a finite value greater than 0."];
        }

        if (errors.Count > 0)
        {
            throw new ApplicationValidationException(errors);
        }

        return repository.FindNearAsync(
            latitude,
            longitude,
            radiusKm,
            cancellationToken);
    }

    public Task<WildfireFeedSyncStateDto?> GetFeedSyncStateAsync(
        CancellationToken cancellationToken = default) =>
        syncStateRepository.GetAsync(cancellationToken);

    public async Task<WildfireSyncResult> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        await RefreshLock.WaitAsync(cancellationToken);

        try
        {
            return await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private async Task<WildfireSyncResult> RefreshCoreAsync(
        CancellationToken cancellationToken)
    {
        var attemptUtc = DateTime.UtcNow;
        WildfireSourceResult sourceResult;

        try
        {
            sourceResult = await source.GetActiveWildfiresAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await syncStateRepository.RecordFailureAsync(
                attemptUtc,
                exception.Message,
                cancellationToken);
            throw;
        }

        var successfulFetchUtc = DateTime.UtcNow;
        await syncStateRepository.RecordFetchSuccessAsync(
            attemptUtc,
            successfulFetchUtc,
            sourceResult.Received,
            sourceResult.Accepted,
            sourceResult.Rejected,
            cancellationToken);

        try
        {
            var (inserted, changed, observed) =
                await repository.SynchronizeAsync(sourceResult.Records, cancellationToken);

            await syncStateRepository.RecordAttemptSuccessAsync(
                attemptUtc,
                cancellationToken);

            return new WildfireSyncResult(
                sourceResult.Received,
                sourceResult.Accepted,
                sourceResult.Rejected,
                inserted,
                changed,
                observed,
                DateTime.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await syncStateRepository.RecordFailureAsync(
                attemptUtc,
                exception.Message,
                cancellationToken);
            throw;
        }
    }
}
