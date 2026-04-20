using Mart.Api.Models;
using Mart.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Domain.Interface
{
    public interface IAdminProductRepository
    {
        Task<int> AddProductAsync(ProductRequest product);
        Task<bool> UpdateProductAsync(ProductRequest product);
        Task<bool> DeleteProductAsync(int productId);


        Task<bool> UpdateStockAsync(int productId, int storeId, int newQuantity);
        Task<bool> BulkSyncToRedisAsync(int storeId);

        Task<Product> GetProductByBarcodeAsync(string barcode);
        Task<Product> GetProductByIdAsync(int id);
        Task<bool> AddProductMediaAsync(ProductMedia media);

        Task<bool> AddVariantAsync(ProductVariantRequest variant);
        Task<IEnumerable<object>> GetVariantsByProductIdAsync(int productId);


        Task<IEnumerable<object>> GetLowStockAlertsAsync();


        Task<object> GetDashboardSummaryAsync();

        Task<int> BulkUpdatePriceAsync(List<int> productIds, decimal percentageChange);


        Task<IEnumerable<object>> GetExpiryAlertsAsync(int daysThreshold);
        Task<bool> MarkBatchAsWasteAsync(int batchId);
    }
}
