using OrderService.Api.DTOs;

namespace OrderService.Api.Services;

public interface ICustomerClient
{
    Task<bool> CustomerExistsAsync(int customerId);

    Task<CustomerDto?> GetCustomerAsync(int customerId);
}