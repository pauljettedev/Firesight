namespace Firesight.Application.AskFiresight;

public interface IAskFiresightService
{
    Task<AskFiresightResult> AskAsync(
        string question,
        CancellationToken cancellationToken = default);
}
