namespace Mart.Api.Models
{
    public class StoreManagerMappingDto
    {
        public int UserId { get; set; }
        public int StoreId { get; set; }
    }

    public class StockApprovalDto
    {
        public int RequestId { get; set; }
        public bool IsApproved { get; set; }
        public string AdminRemarks { get; set; }
    }

}
