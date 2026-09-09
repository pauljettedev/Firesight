namespace Firesight.Application.Locations;

public sealed record GeocodedLocationDto(
    string DisplayName,
    double Latitude,
    double Longitude);
