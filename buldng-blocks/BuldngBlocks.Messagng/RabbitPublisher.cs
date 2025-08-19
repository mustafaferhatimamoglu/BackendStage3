using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BuldngBlocks.Messagng;

public class RabbitPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _conn;
    private readonly string _defaultExchange;

    public RabbitPublisher(RabbitConfig cfg, ILogger<RabbitPublisher> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = cfg.HostName,
            UserName = cfg.UserName,
            Password = cfg.Password,
            DispatchConsumersAsync = true
        };
        _conn = factory.CreateConnection();
        _defaultExchange = cfg.Exchange;
    }

    public Task PublishAsync(string exchange, string routingKey, object message, CancellationToken ct = default)
    {
        using var ch = _conn.CreateModel();
        var ex = string.IsNullOrWhiteSpace(exchange) ? _defaultExchange : exchange;
        ch.ExchangeDeclare(ex, ExchangeType.Topic, durable: true, autoDelete: false);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = ch.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        ch.BasicPublish(ex, routingKey, props, body);
        return Task.CompletedTask;
    }

    public void Dispose() => _conn.Dispose();
}

