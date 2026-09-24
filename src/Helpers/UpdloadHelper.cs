using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace api_finances.src.Helpers
{
    public class UploadHelper(Cloudinary cloudinary)
    {
        private readonly string ApiUri = Environment.GetEnvironmentVariable("API_URI") ?? "";

        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            try
            {
                string extension = Path.GetExtension(file.FileName).ToLower();
                bool isHeic = extension == ".heic" || extension == ".heif";
                string fileName = Guid.NewGuid().ToString();

                using var memoryStream = new MemoryStream();

                if (isHeic)
                {
                    // using var inputStream = file.OpenReadStream();
                    // using var image = await Image.LoadAsync(inputStream);
                    // await image.SaveAsJpegAsync(memoryStream);
                    // memoryStream.Position = 0;
                    // extension = ".jpg";
                }
                else
                {
                }
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName + extension, memoryStream),
                    Folder = $"my-finances/{folder}",
                    PublicId = fileName
                };

                RawUploadResult result = await cloudinary.UploadAsync(uploadParams);

                return result.SecureUrl.ToString();
            }
            catch
            {
                return "";
            }
        }

        public async Task<string> SaveFileV1Async(IFormFile file)
        {
            try
            {
                string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploadDirectory)) Directory.CreateDirectory(uploadDirectory);
                string filePath = Path.Combine(uploadDirectory, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"{ApiUri}/uploads/{file.FileName}";
            }
            catch
            {
                return "";
            }
        }
    }
}
