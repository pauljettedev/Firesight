using Firesight.Application.Common;

namespace Firesight.UnitTests;

public sealed class ApplicationValidationExceptionTests
{
    [Fact]
    public void Constructor_DefensivelyCopiesErrors()
    {
        var messages = new[] { "Original message." };
        var errors = new Dictionary<string, string[]>
        {
            ["field"] = messages
        };

        var exception = new ApplicationValidationException(errors);

        messages[0] = "Changed message.";
        errors["field"] = ["Replacement message."];

        Assert.Equal(["Original message."], exception.Errors["field"]);
    }
}
