using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Fatiha__app.Models.ViewModel
{
    public class AlQeratCreateViewModel
    {
        [SwaggerSchema(Title = "اسم القراءات", Description = "مثال: Qerat Example")]
        [Required]
        [StringLength(100, ErrorMessage = "QeratName cannot exceed 100 characters.")]
        public string QeratName { get; set; }

        [SwaggerSchema(Title = "الوصف", Description = "مثال: This is a sample description.")]
        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        [SwaggerSchema(Title = "ملف الصوت", Description = "ملف صوتي بتنسيق MP3 أو WAV")]
        [Required]
        public IFormFile? AudioFile { get; set; }

        public string? ExistingAudioFile { get; set; }
    }
}
