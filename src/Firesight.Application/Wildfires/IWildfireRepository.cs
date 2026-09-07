namespace Firesight.Application.Wildfires;

public interface IWildfireRepository
{
    Task<IReadOnlyList<WildfireDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<WildfireDto?> GetByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NearbyWildfireDto>> FindNearAsync(
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default);

    Task<(int Inserted, int Changed, int Observed)> SynchronizeAsync(
        IReadOnlyCollection<WildfireImportRecord> wildfires,
        CancellationToken cancellationToken = default);
}
