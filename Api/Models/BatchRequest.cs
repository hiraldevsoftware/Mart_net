using System.ComponentModel.DataAnnotations;

namespace Mart.Api.Models
{
    public class BatchRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
