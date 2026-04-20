namespace Mart.Domain.Entities
{
    public class ProductMedia
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string MediaUrl { get; set; }
        public string MediaType { get; set; } 
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
    }
}
