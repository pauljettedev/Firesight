namespace Firesight.Infrastructure.Claude;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string Model { get; init; } = "claude-haiku-4-5";
    public string ApiKey { get; init; } = string.Empty;
}
