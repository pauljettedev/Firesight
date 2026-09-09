using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Firesight.Application.Locations;

namespace Firesight.Infrastructure.Nominatim;

public sealed class NominatimLocationGeocoder(HttpClient httpClient) : ILocationGeocoder
{
    public async Task<GeocodedLocationDto?> FindAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var encodedQuery = Uri.EscapeDataString(query.Trim());
        var requestUri =
            $"search?q={encodedQuery}&format=jsonv2&limit=1&countrycodes=ca";

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<NominatimSearchResult[]>(
            cancellationToken);

        var result = results?.FirstOrDefault();
        if (result is null ||
            !double.TryParse(
                result.Latitude,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var latitude) ||
            !double.TryParse(
                result.Longitude,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var longitude))
        {
            return null;
        }

        return new GeocodedLocationDto(
            result.DisplayName,
            latitude,
            longitude);
    }

    private sealed record NominatimSearchResult(
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("lat")] string Latitude,
        [property: JsonPropertyName("lon")] string Longitude);
}
