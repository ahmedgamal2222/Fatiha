using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fatiha__app.Models
{
    public class FatihaRequest
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        public IdentityUser ApplicationUser { get; set; }
        public virtual ICollection<PointsLogs> PointsLogs { get; set; } = new List<PointsLogs>();


        [Required]
        [StringLength(255, ErrorMessage = "AudioRecord URL cannot exceed 255 characters.")]
        public string AudioRecord { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfRecord { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "RequestLetter  cannot exceed 500 characters.")]

        public string RequestLetter { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfAccreditation { get; set; }

        public bool IsApproved { get; set; }

        public FatihaRequestStatus Status { get; set; }

        [Required]
        public int? AlQeratId { get; set; }
        public AlQerat AlQerat { get; set; }
        public virtual ICollection<FatihaRequestComment> Comments { get; set; } = new List<FatihaRequestComment>();

        public int? AuthorizedById { get; set; }
        public AuthorizedUsers AuthorizedBy { get; set; }
    }

    public enum FatihaRequestStatus
    {
        Open,
        Processing,
        Closed,
        Qualified
    }
    public class ChangeStatusRequest
    {
        [Required]
        public string Status { get; set; }
    }

}
