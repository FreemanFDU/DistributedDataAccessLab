using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Models;
using OrderService.Api.Services;
using OrderService.Api.Events;
using OrderService.Api.DTOs;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IProductClient _productClient;
    private readonly RabbitMqPublisher _publisher;

    public OrdersController(
        OrdersDbContext context,
        ICustomerClient customerClient,
        IProductClient productClient,
        RabbitMqPublisher publisher)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _publisher = publisher;
    }

    // ✅ 获取全部订单（DTO）
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders.ToListAsync();

        var result = orders.Select(o => new OrderDto
        {
            Id = o.Id,
            CustomerId = o.CustomerId,
            ProductId = o.ProductId,
            Quantity = o.Quantity,
            Status = o.Status
        });

        return Ok(result);
    }

    // ✅ 获取单个订单（⭐新增，替代 details）
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        var result = new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            Status = order.Status
        };

        return Ok(result);
    }

    // ✅ 创建订单（DTO）
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        // ✅ 验证 Customer
        var customerExists = await _customerClient
            .CustomerExistsAsync(dto.CustomerId);

        if (!customerExists)
            return BadRequest("Customer does not exist.");

        // ✅ 验证 Product
        var productExists = await _productClient
            .ProductExistsAsync(dto.ProductId);

        if (!productExists)
            return BadRequest("Product does not exist.");

        // ✅ 创建订单
        var order = new Order
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Status = "Created"
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // ✅ 发布 OrderCreated 事件
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity
        };

        _publisher.Publish(orderEvent);

        var result = new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            Status = order.Status
        };

        return Ok(result);
    }

    // ❌ 已删除 details/{id}（Aggregation 不在这里做）

    // ✅ 取消订单
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound("Order not found.");

        if (order.Status == "Cancelled")
            return BadRequest("Order already cancelled.");

        // ✅ 更新状态
        order.Status = "Cancelled";
        await _context.SaveChangesAsync();

        // ✅ 发布 OrderCancelled 事件
        var cancelEvent = new OrderCancelledEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity
        };

        _publisher.PublishOrderCancelled(cancelEvent);

        Console.WriteLine($"📤 OrderCancelledEvent published for Order {order.Id}");

        var result = new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            Status = order.Status
        };

        return Ok(result);
    }
}