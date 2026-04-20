namespace Mart.Domain.Interface
{
    public interface IAdminBannerRepository
    {
        Task<int> AddBannerAsync(object banner); 
        Task<IEnumerable<object>> GetAllBannersAsync();
        Task<bool> DeleteBannerAsync(int id);
        Task<bool> ToggleBannerStatusAsync(int id, bool isActive);
    }
}
