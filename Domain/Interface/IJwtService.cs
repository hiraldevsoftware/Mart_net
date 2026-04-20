using Mart.Domain.Entities;

namespace Mart.Domain.Interface
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
