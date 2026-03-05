using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderService.Api.Events;

namespace OrderService.Api.Services;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // ✅ 声明 Fanout Exchange
        _channel.ExchangeDeclare(
            exchange: "order-created-exchange",
            type: ExchangeType.Fanout,
            durable: true);

        Console.WriteLine("✅ Publisher connected to RabbitMQ (fanout)");
    }

    public void Publish(OrderCreatedEvent orderEvent)
    {
        var message = JsonSerializer.Serialize(orderEvent);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "order-created-exchange",
            routingKey: "",
            basicProperties: properties,
            body: body);

        Console.WriteLine("📤 OrderCreatedEvent published (fanout).");
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}