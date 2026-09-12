using Anthropic;
using Anthropic.Models.Messages;

namespace Firesight.Infrastructure.Claude;

public sealed class ClaudeMessagesClient(AnthropicClient client) : IClaudeMessagesClient
{
    public Task<Message> CreateAsync(
        MessageCreateParams parameters,
        CancellationToken cancellationToken = default) =>
        client.Messages.Create(parameters, cancellationToken);
}
