using Mart.Domain.Common;

namespace Mart.Domain.Entities
{
    public class User : BaseEntity
    {
        public string? Name { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPhoneVerified { get; set; }
        public string Role { get; set; } = "Customer";
        public decimal WalletBalance { get; set; }

        public string? OtpCode { get; set; }    
        public DateTime? OtpExpiry { get; set; }
    }
}
