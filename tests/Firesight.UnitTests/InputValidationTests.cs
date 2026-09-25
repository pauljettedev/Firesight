using Firesight.Application.AskFiresight;
using Firesight.Application.Common;
using Firesight.Application.Locations;

namespace Firesight.UnitTests;

public sealed class InputValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AskFiresightQuestion_RejectsMissingQuestion(string? question)
    {
        var exception = Assert.Throws<ApplicationValidationException>(
            () => AskFiresightQuestion.Validate(question));

        Assert.Equal(["A question is required."], exception.Errors["question"]);
    }

    [Fact]
    public void AskFiresightQuestion_RejectsQuestionOverMaxLength()
    {
        var question = new string('a', AskFiresightQuestion.MaxLength + 1);

        var exception = Assert.Throws<ApplicationValidationException>(
            () => AskFiresightQuestion.Validate(question));

        Assert.Equal(
            ["Question must be 500 characters or fewer."],
            exception.Errors["question"]);
    }

    [Fact]
    public void AskFiresightQuestion_AcceptsQuestionAtMaxLength()
    {
        AskFiresightQuestion.Validate(new string('a', AskFiresightQuestion.MaxLength));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LocationQuery_RejectsMissingQuery(string? query)
    {
        var exception = Assert.Throws<ApplicationValidationException>(
            () => LocationQuery.Validate(query));

        Assert.Equal(["A town or city is required."], exception.Errors["query"]);
    }

    [Fact]
    public void LocationQuery_AcceptsPlaceName()
    {
        LocationQuery.Validate("Kamloops");
    }
}
