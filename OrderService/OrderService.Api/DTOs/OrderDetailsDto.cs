namespace OrderService.Api.DTOs;

public class OrderDetailsDto
{
    public int OrderId { get; set; }
    public string Status { get; set; }
    public int Quantity { get; set; }
    public CustomerDto Customer { get; set; }
    public ProductDto Product { get; set; }
}