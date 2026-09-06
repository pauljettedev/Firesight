namespace Firesight.Application.Wildfires;

public interface IWildfireSyncStateRepository
{
    Task<WildfireFeedSyncStateDto?> GetAsync(
        CancellationToken cancellationToken = default);

    Task RecordFetchSuccessAsync(
        DateTime attemptUtc,
        DateTime successfulFetchUtc,
        int received,
        int accepted,
        int rejected,
        CancellationToken cancellationToken = default);

    Task RecordAttemptSuccessAsync(
        DateTime attemptUtc,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        DateTime attemptUtc,
        string error,
        CancellationToken cancellationToken = default);
}
