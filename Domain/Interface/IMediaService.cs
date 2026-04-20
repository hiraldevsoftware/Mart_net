namespace Mart.Domain.Interface
{
    public interface IMediaService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task<bool> DeleteImageAsync(string publicId);
    }
}
