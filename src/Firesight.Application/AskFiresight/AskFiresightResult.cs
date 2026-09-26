namespace Firesight.Application.AskFiresight;

// WildfireExternalIds: the CWFIS IDs of the fires the answer is about, in
// display order, so the UI can list them and show them on the map. Claude
// names them, and only IDs that match fires Firesight's own tools returned
// are kept, so the model can't invent one.
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
