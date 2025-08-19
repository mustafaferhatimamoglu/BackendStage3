namespace BuldngBlocks.Messagng;

public interface IMessagePublisher
{
    Task PublishAsync(string exchange, string routingKey, object message, CancellationToken ct = default);
}

