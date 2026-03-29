using System.Net;
using System.Net.Http.Json;
using OrderService.Api.DTOs;

namespace OrderService.Api.Services;

public class ProductClient : IProductClient
{
    private readonly HttpClient _httpClient;

    public ProductClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ProductExistsAsync(int productId)
    {
        var response = await _httpClient
            .GetAsync($"api/products/{productId}");

        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<ProductDto?> GetProductAsync(int productId)
    {
        return await _httpClient
            .GetFromJsonAsync<ProductDto>(
                $"api/products/{productId}");
    }
}