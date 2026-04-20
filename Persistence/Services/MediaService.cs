using Mart.Domain.Interface;

namespace Mart.Persistence.Services
{
    public class MediaService : IMediaService
    {
        private readonly IWebHostEnvironment _environment;


        public MediaService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;


            string rootPath = _environment.WebRootPath ?? _environment.ContentRootPath;

            string uploadsFolder = Path.Combine(rootPath, "wwwroot", "uploads", folder);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{fileName}";
        }

        public Task<bool> DeleteImageAsync(string path)
        {
      
            return Task.FromResult(true);
        }
    }
}
