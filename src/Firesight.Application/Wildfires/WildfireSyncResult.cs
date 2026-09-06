namespace Firesight.Application.Wildfires;

public sealed record WildfireSyncResult(
    int Received,
    int Accepted,
    int Rejected,
    int Inserted,
    int Changed,
    int Observed,
    DateTime CompletedUtc);
