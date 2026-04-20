namespace Mart.Api.Models
{
    public class OneTapRequest
    {
        public int UserId { get; set; }
        public int StoreId { get; set; }

        public int? ProductId { get; set; }
    }
}
