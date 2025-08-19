using BuldngBlocks.Messagng;

namespace Product.Infrastructure.Messagng;

public class RabbtPublsher
{
    private readonly IMessagePublisher _publisher;
    public RabbtPublsher(IMessagePublisher publisher) => _publisher = publisher;

    public Task PublishAsync(string exchange, string routingKey, object message, CancellationToken ct = default)
        => _publisher.PublishAsync(exchange, routingKey, message, ct);
}

