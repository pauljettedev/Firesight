namespace Firesight.Application.Locations;

public interface ILocationGeocoder
{
    Task<GeocodedLocationDto?> FindAsync(
        string query,
        CancellationToken cancellationToken = default);
}
