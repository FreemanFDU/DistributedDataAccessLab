namespace ProductService.Api.Events;

public class OrderCreatedEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}