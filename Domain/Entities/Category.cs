using Mart.Domain.Common;

namespace Mart.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
