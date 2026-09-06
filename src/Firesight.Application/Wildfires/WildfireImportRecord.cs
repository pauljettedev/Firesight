namespace Firesight.Application.Wildfires;

public sealed record WildfireImportRecord(
    string ExternalId,
    string Agency,
    string? Name,
    double Latitude,
    double Longitude,
    DateTime? StartDate,
    double? AreaHectares,
    string Status,
    DateTime? StatusDateUtc);
