using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaymentService.Api.Data;
using PaymentService.Api.Events;

namespace PaymentService.Api.Services;

public class OrderCancelledConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderCancelledConsumer(IServiceScopeFactory scopeFactory)
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
                Console.WriteLine("🔄 PaymentService connecting (Cancel)...");

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // ✅ 声明 exchange
                _channel.ExchangeDeclare(
                    exchange: "order-cancelled-exchange",
                    type: ExchangeType.Fanout,
                    durable: true);

                // ✅ 声明队列
                _channel.QueueDeclare(
                    queue: "payment-cancel-queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // ✅ 绑定
                _channel.QueueBind(
                    queue: "payment-cancel-queue",
                    exchange: "order-cancelled-exchange",
                    routingKey: "");

                Console.WriteLine("✅ PaymentService connected for OrderCancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ RabbitMQ not ready (Payment Cancel)");
                Console.WriteLine($"Error: {ex.Message}");
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
                var cancelEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(json);

                if (cancelEvent != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                    var payment = await db.Payments
                        .FirstOrDefaultAsync(p => p.OrderId == cancelEvent.OrderId);

                    if (payment != null)
                    {
                        payment.Status = "Cancelled";
                        await db.SaveChangesAsync();

                        Console.WriteLine($"✅ Payment cancelled for Order {cancelEvent.OrderId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Payment Cancel error: {ex.Message}");
            }
        };

        _channel.BasicConsume(
            queue: "payment-cancel-queue",
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