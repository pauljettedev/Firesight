namespace Firesight.Application.AskFiresight;

public sealed record AskFiresightResult(
    string Answer,
    IReadOnlyList<string> ToolsUsed);
