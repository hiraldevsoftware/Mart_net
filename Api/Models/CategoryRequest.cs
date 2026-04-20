namespace Mart.Api.Models
{
    public class CategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
