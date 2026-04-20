namespace Mart.Api.Models
{
    public class UpdateStageDto
    {
        public int OrderId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
    }
}
