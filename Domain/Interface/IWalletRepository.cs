namespace Mart.Domain.Interface
{
    public interface IWalletRepository
    {
        Task<decimal>GetWalletBalanceAsync(int userId);
        Task UpdateWalletBalanceAsync(int userId, decimal amount);
    }
}
