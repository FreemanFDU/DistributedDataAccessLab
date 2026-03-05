using System.Net;
using OrderService.Api.Services; // 加上这一行

namespace OrderService.Api.Services; // 重要：这行就是你报错的原因！

public class CustomerClient : ICustomerClient
{
    private readonly HttpClient _httpClient;
    public CustomerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<bool> CustomerExistsAsync(int customerId)
    {
        // 这里的路径要和 CustomerService 对应
        var response = await _httpClient.GetAsync($"api/customers/{customerId}");
        return response.StatusCode == HttpStatusCode.OK;
    }
}