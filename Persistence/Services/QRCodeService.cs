using Mart.Domain.Interface;
using QRCoder;

namespace Mart.Persistence.Services
{
    public class QRCodeService : IQRCodeService
    {
        public string GenerateQRCodeBase64(string text)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);
                return Convert.ToBase64String(qrCodeAsPngByteArr);
            }
        }
    }
}
