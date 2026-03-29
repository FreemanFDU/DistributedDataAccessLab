namespace PaymentService.Api.Models;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public string Status { get; set; } = "Processed";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}