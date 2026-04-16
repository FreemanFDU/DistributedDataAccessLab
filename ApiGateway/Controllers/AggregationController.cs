using Microsoft.AspNetCore.Mvc;
using ApiGateway.DTOs;

namespace ApiGateway.Controllers;

[ApiController]
[Route("aggregation/order-details")]
public class AggregationController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public AggregationController(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var order = await _httpClient.GetFromJsonAsync<OrderDto>(
                $"http://orderservice:8080/api/orders/{id}");

            if (order == null)
                return NotFound("Order not found");

            var customer = await _httpClient.GetFromJsonAsync<CustomerDto>(
                $"http://customerservice:8080/api/customers/{order.CustomerId}");

            var product = await _httpClient.GetFromJsonAsync<ProductDto>(
                $"http://productservice:8080/api/products/{order.ProductId}");

            return Ok(new
            {
                orderId = order.Id,
                status = order.Status,
                quantity = order.Quantity,
                customer,
                product
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}