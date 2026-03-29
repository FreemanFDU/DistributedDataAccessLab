namespace OrderService.Api.Models;

public class Order
{
    public int Id { get; set; }

    public decimal Total { get; set; }

    public int CustomerId { get; set; }

    // ✅ 新增
    public int ProductId { get; set; }

    public int Quantity { get; set; }
    
    public string Status { get; set; } = "Pending";
}