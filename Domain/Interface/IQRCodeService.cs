namespace Mart.Domain.Interface
{
    public interface IQRCodeService
    {
        string GenerateQRCodeBase64(string text);
    }
}
