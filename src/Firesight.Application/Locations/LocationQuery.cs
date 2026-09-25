using System.Diagnostics.CodeAnalysis;
using Firesight.Application.Common;

namespace Firesight.Application.Locations;

public static class LocationQuery
{
    public static void Validate([NotNull] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ApplicationValidationException(
                new Dictionary<string, string[]>
                {
                    [nameof(query)] = ["A town or city is required."]
                });
        }
    }
}
