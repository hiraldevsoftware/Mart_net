namespace Mart.Api.Models
{
    public class CartRequest
    {

        public int UserId { get; set; }

        public int ProductId { get; set; }


        public int Quantity { get; set; }

        public int StoreId { get; set; }
    }
}
