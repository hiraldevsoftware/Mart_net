using Mart.Domain.Common;
using Mart.Domain.ValueObjects;

namespace Mart.Api.Models
{
    public class ProductRequest : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Money Price { get; set; }
        public int StockCount { get; set; }

        public string? Barcode { get; set; }

        public string? QRCodeBase64 { get; set; }
        public int MinStockAlert { get; set; }
        public Guid CategoryId { get; set; }

        public int Stock { get; set; } 
        public string? ImageUrl { get; set; }

        public void UpdateStock(int quantity)
        {
            if (StockCount + quantity < 0)
                throw new Exception("Insufficient stock!");
            StockCount += quantity;
        }
    }
}
