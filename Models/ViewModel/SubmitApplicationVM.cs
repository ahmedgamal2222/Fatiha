using Microsoft.AspNetCore.Http;

namespace Fatiha__app.Models.ViewModel
{
    public class SubmitApplicationVM
    {
        public string AuthorizedUser { get; set; } // استقبال كـ string ثم تحويلها لكائن
        public IFormFile AudioFile { get; set; }
        public IFormFile CvFile { get; set; }
        public IFormFile ImgFile { get; set; }
    }

}
