namespace OrderService.Api.Services;

public interface IProductClient
{
    Task<bool> ProductExistsAsync(int productId);

    // ✅ 新增
    Task<bool> DecreaseStockAsync(int productId, int quantity);
}