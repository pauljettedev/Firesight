using Anthropic.Models.Messages;

namespace Firesight.Infrastructure.Claude;

public interface IClaudeMessagesClient
{
    Task<Message> CreateAsync(
        MessageCreateParams parameters,
        CancellationToken cancellationToken = default);
}
