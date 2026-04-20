namespace Mart.Api.Models
{
    public class CheckoutRequest
    {
        public int UserId { get; set; }
        public int StoreId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartItemDto> Items { get; set; }
    }
}
