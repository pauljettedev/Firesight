namespace Firesight.Application.AskFiresight;

// WildfireExternalIds: the CWFIS IDs of the fires the answer mentions, in the
// order they're mentioned, so the UI can show them on the map. Only IDs that
// came back from Firesight's own tools count, so the model can't invent one.
public sealed record AskFiresightResult(
    string Answer,
    IReadOnlyList<string> ToolsUsed,
    AskFiresightMapContext? MapContext,
    IReadOnlyList<string> WildfireExternalIds);

public sealed record AskFiresightMapContext(
    double Latitude,
    double Longitude,
    double RadiusKm,
    string? Label);
