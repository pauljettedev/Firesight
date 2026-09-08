using System.Collections.ObjectModel;

namespace Firesight.Application.Common;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(
        IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "At least one validation error is required.",
                nameof(errors));
        }

        var copiedErrors = errors.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)Array.AsReadOnly(pair.Value.ToArray()),
            StringComparer.Ordinal);

        Errors = new ReadOnlyDictionary<string, IReadOnlyList<string>>(copiedErrors);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }
}
