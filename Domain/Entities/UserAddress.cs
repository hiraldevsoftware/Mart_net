namespace Mart.Domain.Entities
{
    public class UserAddress
    {
        public Guid Id { get; set; }
        public int UserId { get; set; } 
        public string FullAddress { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Tag { get; set; } = "Home"; 
        public bool IsDefault { get; set; }
    }
}
