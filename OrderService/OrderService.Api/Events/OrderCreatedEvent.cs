namespace OrderService.Api.Events;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }   // 👈 加上这一行
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}