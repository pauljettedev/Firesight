namespace Firesight.Application.Wildfires;

public interface IWildfireSource
{
    Task<WildfireSourceResult> GetActiveWildfiresAsync(
        CancellationToken cancellationToken = default);
}
