namespace Mart.Api.Models
{
    public class AuditDto
    {
        public int StoreId { get; set; }
        public int ProductId { get; set; }
        public int PhysicalQty { get; set; }
        public int StaffId { get; set; }
        public string Remarks { get; set; }
    }
}
