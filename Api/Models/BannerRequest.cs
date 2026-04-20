namespace Mart.Api.Models
{
    public class BannerRequest
    {
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? LinkType { get; set; }
        public string? LinkId { get; set; }
    }
}
