using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.DTOs;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _context;

    public PaymentsController(PaymentDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _context.Payments.ToListAsync();

        var result = payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            OrderId = p.OrderId,
            Status = p.Status
        });

        return Ok(result);
    }
}