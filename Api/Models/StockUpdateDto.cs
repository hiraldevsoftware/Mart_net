namespace Mart.Api.Models
{
    public class StockUpdateDto
    {
        public int StoreId { get; set; }
        public int ProductId { get; set; }
        public int NewQuantity { get; set; }
        public int StaffId { get; set; } 
        public string Reason { get; set; } 
    }
}
