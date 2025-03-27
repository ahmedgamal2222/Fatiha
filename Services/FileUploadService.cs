namespace Fatiha__app.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public FileUploadService(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subDirectory)
        {
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var webRootPath = _hostEnvironment.WebRootPath;
                var filePath = Path.Combine(webRootPath, subDirectory, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Returning the relative path or the accessible URL (as per your requirements)
                return Path.Combine(subDirectory, fileName);
            }

            return null;
        }
    }

}
