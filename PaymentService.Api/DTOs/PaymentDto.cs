namespace PaymentService.Api.DTOs;

public class PaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string? Status { get; set; }
}