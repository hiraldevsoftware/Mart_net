namespace Mart.Api.Models
{
    public class HomeSection
    {
        public string SectionType { get; set; } = string.Empty; 
        public string Title { get; set; } = string.Empty;
        public object Data { get; set; } = new();
    }

    public class HomeLayoutResponse
    {
        public List<HomeSection> Sections { get; set; } = new();
    }
}
