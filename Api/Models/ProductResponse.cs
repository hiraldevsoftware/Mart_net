namespace Mart.Api.Models
{
    public class ProductResponse
    {

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "INR";
        public int StockCount { get; set; }
        public Guid CategoryId { get; set; }
    }
}
