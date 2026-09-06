namespace Firesight.Application.Wildfires;

public sealed record WildfireFeedSyncStateDto(
    DateTime LastAttemptUtc,
    DateTime? LastSuccessfulFetchUtc,
    bool LastAttemptSucceeded,
    int Received,
    int Accepted,
    int Rejected,
    string? LastError);
