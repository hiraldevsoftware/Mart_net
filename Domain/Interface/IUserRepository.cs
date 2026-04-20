using Mart.Domain.Entities;

namespace Mart.Persistence.Repositories
{
    public interface IUserRepository
    {
        Task<int> CreateUserAsync(User user);
        Task<User?> GetUserByPhoneAsync(string phoneNumber);

        Task UpdateUserAsync(User user);

        Task UpdateFcmTokenAsync(int userId, string token);
    }
}
