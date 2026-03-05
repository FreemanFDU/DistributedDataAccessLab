namespace ProductService.Api.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }   // 重要：后面 RabbitMQ 会更新这个
}