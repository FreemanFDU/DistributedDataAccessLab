using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Models;
using OrderService.Api.Services;
using OrderService.Api.Events;

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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders.ToListAsync();
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Order order)
    {
        // ✅ 1️⃣ 验证 Customer
        var customerExists = await _customerClient
            .CustomerExistsAsync(order.CustomerId);

        if (!customerExists)
            return BadRequest("Customer does not exist.");

        // ✅ 2️⃣ 验证 Product 是否存在
        var productExists = await _productClient
            .ProductExistsAsync(order.ProductId);

        if (!productExists)
            return BadRequest("Product does not exist.");

        // ✅ ❌ 删除同步扣库存
        // 不再调用 DecreaseStockAsync
        // 库存更新通过 RabbitMQ 事件完成

        // ✅ 3️⃣ 保存订单
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // ✅ 4️⃣ 发布事件
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity
        };

        _publisher.Publish(orderEvent);

        return Ok(order);
    }
}