namespace Firesight.Application.Wildfires;

public interface IWildfireService
{
    Task<IReadOnlyList<WildfireDto>> GetActiveWildfiresAsync(
        CancellationToken cancellationToken = default);

    Task<WildfireDto?> GetWildfireByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default);

    Task<WildfireFeedSyncStateDto?> GetFeedSyncStateAsync(
        CancellationToken cancellationToken = default);

    Task<WildfireSyncResult> RefreshAsync(
        CancellationToken cancellationToken = default);
}
