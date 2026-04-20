namespace Mart.Api.Models
{
    public class StockRequestDto
    {
        public int StoreId { get; set; }
        public int ProductId { get; set; }
        public int Qty { get; set; }
        public int StaffId { get; set; }
    }
}
