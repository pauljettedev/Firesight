namespace Firesight.Application.AskFiresight;

public sealed record AskFiresightResult(
    string Answer,
    IReadOnlyList<string> ToolsUsed,
    AskFiresightMapContext? MapContext);

public sealed record AskFiresightMapContext(
    double Latitude,
    double Longitude,
    double RadiusKm,
    string? Label);
