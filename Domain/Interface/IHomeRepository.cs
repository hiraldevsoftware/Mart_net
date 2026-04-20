using Mart.Api.Models;

namespace Mart.Domain.Interface
{
    public interface IHomeRepository
    {
        Task<HomeLayoutResponse> GetHomeLayoutAsync(int userId, double lat, double lon);
    }
}
