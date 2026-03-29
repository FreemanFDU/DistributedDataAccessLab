using OrderService.Api.DTOs;

namespace OrderService.Api.Services;

public interface IProductClient
{
    Task<bool> ProductExistsAsync(int productId);

    Task<ProductDto?> GetProductAsync(int productId);
}