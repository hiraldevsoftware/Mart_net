namespace Mart.Domain.Interface
{
    public interface IAdminSupportRepository
    {
        Task<bool> SetMaintenanceModeAsync(bool isOn);
        Task<bool> IsMaintenanceModeOnAsync();
        Task<bool> UpdateTaxRateAsync(int taxId, decimal newRate);
    }
}
