namespace Mart.Domain.Entities
{
    public class OrderItemDetail
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
