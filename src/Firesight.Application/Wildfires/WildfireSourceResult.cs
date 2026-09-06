namespace Firesight.Application.Wildfires;

public sealed record WildfireSourceResult(
    IReadOnlyList<WildfireImportRecord> Records,
    int Received,
    int Accepted,
    int Rejected);
