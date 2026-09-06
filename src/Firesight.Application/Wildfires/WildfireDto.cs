namespace Firesight.Application.Wildfires;

public sealed record WildfireDto(
    Guid Id,
    string ExternalId,
    string Agency,
    string? Name,
    double Latitude,
    double Longitude,
    DateTime? StartDate,
    double? AreaHectares,
    string Status,
    DateTime? StatusDateUtc,
    DateTime LastSeenInFeedUtc,
    bool IsStale);
