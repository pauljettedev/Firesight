namespace Firesight.Application.Wildfires;

public sealed record NearbyWildfireDto(
    WildfireDto Wildfire,
    double DistanceKm);
