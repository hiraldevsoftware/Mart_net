namespace Mart.Api.Models
{
    namespace JPMart.Models.DTOs
    {
        public class BulkUpdateDto
        {

            public List<int> ProductIds { get; set; } = new List<int>();

            public decimal Percentage { get; set; }

        }
    }
}
