using System.Globalization;
using System.Text.Json;
using Firesight.Application.Wildfires;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Firesight.Infrastructure.Cwfis;

public sealed class CwfisWildfireSource(
    HttpClient httpClient,
    IOptions<CwfisOptions> options,
    ILogger<CwfisWildfireSource> logger) : IWildfireSource
{
    private static readonly string[] AgencyFields = ["agency_code"];
    private static readonly string[] ExternalIdFields = ["national_fire_id"];
    private static readonly string[] LatitudeFields = ["latitude"];
    private static readonly string[] LongitudeFields = ["longitude"];
    private static readonly string[] AreaFields = ["fire_size"];
    private static readonly string[] StatusFields = ["stage_of_control_status"];
    private static readonly string[] StatusDateFields = ["status_date"];

    private readonly CwfisOptions _options = options.Value;

    public async Task<WildfireSourceResult> GetActiveWildfiresAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<WildfireImportRecord>();
        var received = 0;

        using var response = await httpClient.GetAsync(
            BuildRequestUri(),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;
        if (!root.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "CWFIS response did not contain a GeoJSON features array.");
        }

        foreach (var feature in features.EnumerateArray())
        {
            received++;
            var wildfire = ParseFeature(feature, out var rejectionReason);
            if (wildfire is not null)
            {
                result.Add(wildfire);
                continue;
            }

            logger.LogWarning(
                "Rejected CWFIS feature {FeatureId}: {Reason}.",
                GetFeatureId(feature) ?? "(unknown)",
                rejectionReason);
        }

        return new WildfireSourceResult(
            result,
            received,
            result.Count,
            received - result.Count);
    }

    private string BuildRequestUri()
    {
        var query = new Dictionary<string, string>
        {
            ["service"] = "WFS",
            ["version"] = "2.0.0",
            ["request"] = "GetFeature",
            ["typeNames"] = _options.ActiveFiresLayer,
            ["outputFormat"] = "application/json",
            ["srsName"] = "EPSG:4326",
            ["count"] = _options.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        var queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"{_options.BaseUrl}?{queryString}";
    }

    private static WildfireImportRecord? ParseFeature(
        JsonElement feature,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (!feature.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            rejectionReason = "missing or invalid properties object";
            return null;
        }

        if (!TryGetCoordinates(feature, properties, out var longitude, out var latitude))
        {
            rejectionReason = "missing or invalid coordinates";
            return null;
        }

        var externalId = GetString(properties, ExternalIdFields);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            rejectionReason = "missing national_fire_id";
            return null;
        }

        var agency = GetString(properties, AgencyFields) ?? "Unknown";
        var area = GetDouble(properties, AreaFields);
        var status = GetString(properties, StatusFields) ?? string.Empty;
        var statusDateUtc = GetDateTime(properties, StatusDateFields);

        return new WildfireImportRecord(
            externalId,
            agency,
            null,
            latitude,
            longitude,
            null,
            area,
            status,
            statusDateUtc is null
                ? null
                : DateTime.SpecifyKind(statusDateUtc.Value, DateTimeKind.Utc));
    }

    private static string? GetFeatureId(JsonElement feature)
    {
        if (!feature.TryGetProperty("id", out var id))
        {
            return null;
        }

        return id.ValueKind switch
        {
            JsonValueKind.String => id.GetString(),
            JsonValueKind.Number => id.GetRawText(),
            _ => null
        };
    }

    private static bool TryGetCoordinates(
        JsonElement feature,
        JsonElement properties,
        out double longitude,
        out double latitude)
    {
        longitude = 0;
        latitude = 0;

        if (feature.TryGetProperty("geometry", out var geometry) &&
            geometry.ValueKind == JsonValueKind.Object &&
            geometry.TryGetProperty("coordinates", out var coordinates) &&
            coordinates.ValueKind == JsonValueKind.Array &&
            coordinates.GetArrayLength() >= 2 &&
            TryGetDouble(coordinates[0], out longitude) &&
            TryGetDouble(coordinates[1], out latitude))
        {
            return IsValidCoordinate(longitude, latitude);
        }

        var propertyLatitude = GetDouble(properties, LatitudeFields);
        var propertyLongitude = GetDouble(properties, LongitudeFields);
        if (propertyLatitude is null || propertyLongitude is null)
        {
            return false;
        }

        latitude = propertyLatitude.Value;
        longitude = propertyLongitude.Value;
        return IsValidCoordinate(longitude, latitude);
    }

    private static bool IsValidCoordinate(double longitude, double latitude) =>
        longitude is >= -180 and <= 180 && latitude is >= -90 and <= 90;

    private static string? GetString(JsonElement properties, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!TryGetPropertyIgnoreCase(properties, name, out var value))
            {
                continue;
            }

            var result = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(result))
            {
                return result.Trim();
            }
        }

        return null;
    }

    private static double? GetDouble(JsonElement properties, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyIgnoreCase(properties, name, out var value) &&
                TryGetDouble(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static DateTime? GetDateTime(JsonElement properties, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!TryGetPropertyIgnoreCase(properties, name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(
                    value.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryGetDouble(JsonElement value, out double result)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out result))
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(
                value.GetString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    private static bool TryGetInt(JsonElement value, out int result)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out result))
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string name,
        out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }


}
