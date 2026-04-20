namespace Mart.Api.Models
{
    public class RiderRequest
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
 
        public string? VehicleNumber { get; set; }
    }
}
