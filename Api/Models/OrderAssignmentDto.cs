namespace Mart.Api.Models
{
    public class OrderAssignmentDto
    {
        public int OrderId { get; set; }
        public int RiderId { get; set; }
        public string Status { get; set; } 
        public DateTimeOffset AssignmentTime { get; set; }
    }
}
