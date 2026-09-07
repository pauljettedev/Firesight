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
        if (!double.IsFinite(latitude))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (!double.IsFinite(longitude))
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }

        if (!double.IsFinite(radiusKm))
        {
            throw new ArgumentOutOfRangeException(nameof(radiusKm));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(radiusKm, 0);

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
