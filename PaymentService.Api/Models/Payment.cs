namespace PaymentService.Api.Models;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}