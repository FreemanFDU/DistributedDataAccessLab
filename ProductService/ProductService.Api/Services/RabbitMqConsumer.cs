using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using ProductService.Api.Data;
using ProductService.Api.Events;

namespace ProductService.Api.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RabbitMqConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            DispatchConsumersAsync = true
        };

        IConnection? connection = null;
        IModel? channel = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                Console.WriteLine("✅ ProductService connected to RabbitMQ");
                break;
            }
            catch
            {
                Console.WriteLine("⏳ RabbitMQ not ready (ProductService), retrying...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (connection == null || channel == null)
            return;

        // ✅ 声明 exchange
        channel.ExchangeDeclare(
            exchange: "order-created-exchange",
            type: ExchangeType.Fanout,
            durable: true);

        // ✅ 专属队列
        channel.QueueDeclare(
            queue: "product-queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        // ✅ 绑定
        channel.QueueBind(
            queue: "product-queue",
            exchange: "order-created-exchange",
            routingKey: "");

        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

                if (orderEvent == null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var product = await db.Products
                    .FirstOrDefaultAsync(p => p.Id == orderEvent.ProductId);

                if (product != null)
                {
                    product.Stock -= orderEvent.Quantity;
                    await db.SaveChangesAsync();
                    Console.WriteLine($"✅ Stock updated for Product {product.Id}");
                }

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProductService error: {ex.Message}");
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: "product-queue",
            autoAck: false,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}