namespace Mart.Api.Models
{
    public class ProductVariantRequest
    {
        public int ProductId { get; set; }
        public string VariantName { get; set; } = string.Empty; 
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? SKU { get; set; }
    }
}
