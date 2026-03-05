using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaymentService.Api.Data;
using PaymentService.Api.Models;
using PaymentService.Api.Events;

namespace PaymentService.Api.Services;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // ✅ 声明 exchange
        _channel.ExchangeDeclare(
            exchange: "order-created-exchange",
            type: ExchangeType.Fanout,
            durable: true);

        // ✅ 专属队列
        _channel.QueueDeclare(
            queue: "payment-queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        // ✅ 绑定
        _channel.QueueBind(
            queue: "payment-queue",
            exchange: "order-created-exchange",
            routingKey: "");

        Console.WriteLine("✅ PaymentService connected to RabbitMQ (fanout)");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            if (orderEvent != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                var payment = new Payment
                {
                    OrderId = orderEvent.OrderId,
                    CreatedAt = DateTime.UtcNow
                };

                db.Payments.Add(payment);
                await db.SaveChangesAsync();

                Console.WriteLine($"✅ Payment processed for Order {orderEvent.OrderId}");
            }
        };

        _channel.BasicConsume(
            queue: "payment-queue",
            autoAck: true,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}