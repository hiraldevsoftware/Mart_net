using Mart.Domain.Common;
using Mart.Domain.ValueObjects;

namespace Mart.Domain.Entities
{
    public class Product:BaseEntity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Money Price { get; private set; }
        public string? Barcode { get; set; }
        public int StockCount { get; private set; }
        public int MinStockAlert { get; private set; }
        public Guid CategoryId { get; private set; }


        public void UpdateStock(int quantity)
        {
            if (StockCount + quantity < 0)
                throw new Exception("Insufficient stock!");
            StockCount += quantity;
        }

    }
}
