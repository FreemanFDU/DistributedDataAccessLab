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

    // ✅ 获取全部订单（返回 DTO）
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

    // ✅ 创建订单（接收 DTO）
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        // 1️⃣ 验证 Customer
        var customerExists = await _customerClient
            .CustomerExistsAsync(dto.CustomerId);

        if (!customerExists)
            return BadRequest("Customer does not exist.");

        // 2️⃣ 验证 Product
        var productExists = await _productClient
            .ProductExistsAsync(dto.ProductId);

        if (!productExists)
            return BadRequest("Product does not exist.");

        // 3️⃣ 创建实体（DTO → Entity）
        var order = new Order
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Status = "Created"
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // 4️⃣ 发布 OrderCreated 事件
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity
        };

        _publisher.Publish(orderEvent);

        // ✅ 返回 DTO（不要返回实体）
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


    [HttpGet("details/{id}")]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        // ✅ 调用 CustomerService
        var customer = await _customerClient
            .GetCustomerAsync(order.CustomerId);

        // ✅ 调用 ProductService
        var product = await _productClient
            .GetProductAsync(order.ProductId);

        var result = new OrderDetailsDto
        {
            OrderId = order.Id,
            Status = order.Status,
            Quantity = order.Quantity,
            Customer = customer,
            Product = product
        };

        return Ok(result);
    }


    // ✅ 取消订单（返回 DTO）
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

        // ✅ 返回 DTO
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