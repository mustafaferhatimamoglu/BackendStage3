namespace BuldngBlocks.Messagng;

public class RabbitConfig
{
    public string HostName { get; set; } = "rabbitmq";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "products.exchange";
}

