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
            UserName = "guest",
            Password = "guest",
            DispatchConsumersAsync = true
        };

        IConnection? connection = null;
        IModel? channel = null;

        // ✅ 等待 RabbitMQ 启动
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

        // =====================================================
        // ✅ ORDER CREATED
        // =====================================================

        channel.ExchangeDeclare(
            exchange: "order-created-exchange",
            type: ExchangeType.Fanout,
            durable: true);

        channel.QueueDeclare(
            queue: "product-queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: "product-queue",
            exchange: "order-created-exchange",
            routingKey: "");

        // =====================================================
        // ✅ ✅ ORDER CANCELLED (新增部分)
        // =====================================================

        channel.ExchangeDeclare(
            exchange: "order-cancelled-exchange",
            type: ExchangeType.Fanout,
            durable: true);

        channel.QueueDeclare(
            queue: "product-cancel-queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: "product-cancel-queue",
            exchange: "order-cancelled-exchange",
            routingKey: "");

        channel.BasicQos(0, 1, false);

        // =====================================================
        // ✅ Consumer 1 — OrderCreated（扣库存）
        // =====================================================

        var createdConsumer = new AsyncEventingBasicConsumer(channel);

        createdConsumer.Received += async (model, ea) =>
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

                    Console.WriteLine($"✅ Stock decreased for Product {product.Id}");
                }

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProductService error (Created): {ex.Message}");
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: "product-queue",
            autoAck: false,
            consumer: createdConsumer);

        // =====================================================
        // ✅ Consumer 2 — OrderCancelled（恢复库存）
        // =====================================================

        var cancelConsumer = new AsyncEventingBasicConsumer(channel);

        cancelConsumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var cancelEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(json);

                if (cancelEvent == null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var product = await db.Products
                    .FirstOrDefaultAsync(p => p.Id == cancelEvent.ProductId);

                if (product != null)
                {
                    product.Stock += cancelEvent.Quantity;
                    await db.SaveChangesAsync();

                    Console.WriteLine($"✅ Stock restored for Product {product.Id}");
                }

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProductService error (Cancelled): {ex.Message}");
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: "product-cancel-queue",
            autoAck: false,
            consumer: cancelConsumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}