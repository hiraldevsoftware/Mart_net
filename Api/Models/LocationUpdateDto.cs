namespace Mart.Api.Models
{
    public class LocationUpdateDto
    {
        public int ProductId { get; set; }
        public int StoreId { get; set; }
        public string RackNumber { get; set; }
        public string ShelfLevel { get; set; }
    }
}
