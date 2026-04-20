using Mart.Domain.Entities;

namespace Mart.Domain.Interface
{
    public interface IAddressRepository
    {
        Task<bool> AddAddressAsync(UserAddress address);
        Task<IEnumerable<UserAddress>> GetUserAddressesAsync(int userId);
        Task<bool> SetDefaultAddressAsync(int userId, Guid addressId);

        Task<bool> DeleteAddressAsync(int userId, Guid addressId);
    }
}
