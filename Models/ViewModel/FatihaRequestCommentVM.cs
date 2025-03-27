using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fatiha__app.Models.ViewModel
{
    public class FatihaRequestCommentVM
    {
        [Required]
        public int FatihaRequestId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string CommentText { get; set; } // نص التعليق

        public string? AudioRecord { get; set; } // رابط الملف الصوتي بعد الرفع

        [DataType(DataType.Upload)]
        public IFormFile? AudioFile { get; set; } // ملف الصوت الذي سيتم رفعه
    }
}
