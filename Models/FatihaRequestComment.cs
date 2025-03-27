using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fatiha__app.Models
{
    public class FatihaRequestComment
    {
        public int Id { get; set; }

        [ForeignKey("FatihaRequestId")]
        [Required]
        public int FatihaRequestId { get; set; }
        public virtual FatihaRequest FatihaRequest { get; set; }
        [ForeignKey("ApplicationUserId")]

        public string ApplicationUserId { get; set; }
        public IdentityUser ApplicationUser { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfRecord { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string Comment { get; set; }

        public bool isdeleted { get; set; }

        [StringLength(255, ErrorMessage = "AudioRecord URL cannot exceed 255 characters.")]
        public string? AudioRecord { get; set; }

    }
}
