using System.Net;
using System.Net.Http.Json;
using OrderService.Api.DTOs;

namespace OrderService.Api.Services;

public class CustomerClient : ICustomerClient
{
    private readonly HttpClient _httpClient;

    public CustomerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CustomerExistsAsync(int customerId)
    {
        var response = await _httpClient
            .GetAsync($"api/customers/{customerId}");

        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<CustomerDto?> GetCustomerAsync(int customerId)
    {
        return await _httpClient
            .GetFromJsonAsync<CustomerDto>(
                $"api/customers/{customerId}");
    }
}