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
    }

    private void InitializeRabbitMq()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest",
            DispatchConsumersAsync = true
        };

        while (_connection == null)
        {
            try
            {
                Console.WriteLine("🔄 Trying to connect to RabbitMQ...");

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // ✅ Declare exchange
                _channel.ExchangeDeclare(
                    exchange: "order-created-exchange",
                    type: ExchangeType.Fanout,
                    durable: true);

                // ✅ Declare queue
                _channel.QueueDeclare(
                    queue: "payment-queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // ✅ Bind queue
                _channel.QueueBind(
                    queue: "payment-queue",
                    exchange: "order-created-exchange",
                    routingKey: "");

                Console.WriteLine("✅ PaymentService connected to RabbitMQ (fanout)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ RabbitMQ not ready yet.");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("⏳ Retrying in 5 seconds...");
                Thread.Sleep(5000);
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeRabbitMq();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
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
                    Status = "Processed",
                    CreatedAt = DateTime.UtcNow
                };

                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();

                    Console.WriteLine($"✅ Payment processed for Order {orderEvent.OrderId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");
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