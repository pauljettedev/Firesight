using System.Diagnostics.CodeAnalysis;
using Firesight.Application.Common;

namespace Firesight.Application.AskFiresight;

public static class AskFiresightQuestion
{
    // Keeps each Claude request, and its cost, bounded.
    public const int MaxLength = 500;

    public static void Validate([NotNull] string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw Invalid("A question is required.");
        }

        if (question.Length > MaxLength)
        {
            throw Invalid($"Question must be {MaxLength} characters or fewer.");
        }
    }

    private static ApplicationValidationException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["question"] = [message] });
}
