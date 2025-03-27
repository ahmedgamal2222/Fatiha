using Fatiha__app.Extension;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Fatiha__app.Models;
namespace Fatiha__app.Models
{
    public class AuthorizedUsers
    {

        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        public IdentityUser ApplicationUser { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfRequest { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfAuthorization { get; set; }

        public string? AuthorizedByUserId { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]

        public string Description { get; set; }


        public string AudioRecordingRequest { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsOnline { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastLoginDate { get; set; }

        public int Points { get; set; }

        public int AlQeratId { get; set; }
        public AlQerat AlQerats { get; set; }

        // new start from here
        [StringLength(1000, ErrorMessage = "BriefOverview cannot exceed 1000 characters.")]
        public string BriefOverview { get; set; }

        [StringLength(500, ErrorMessage = "AcademicQualifications cannot exceed 500 characters.")]
        public string AcademicQualifications { get; set; }


        public AcademicAttainment AcademicAttainment { get; set; }

        public CurrentPosition CurrentPosition { get; set; }

        [StringLength(300, ErrorMessage = "Cv cannot exceed 300 characters.")]
        public string Cv { get; set; }

        [StringLength(300, ErrorMessage = "ProfileImg cannot exceed 300 characters.")]
        public string ProfileImg { get; set; }

        [StringLength(300, ErrorMessage = "Facebook cannot exceed 300 characters.")]
        public string Facebook { get; set; }

        [StringLength(300, ErrorMessage = "Twitter cannot exceed 300 characters.")]
        public string Twitter { get; set; }

        [StringLength(300, ErrorMessage = "LinkedIn cannot exceed 300 characters.")]
        public string LinkedIn { get; set; }

        [StringLength(300, ErrorMessage = "TikTok cannot exceed 300 characters.")]
        public string TikTok { get; set; }

        [StringLength(300, ErrorMessage = "Instagram cannot exceed 300 characters.")]
        public string Instagram { get; set; }

        [StringLength(300, ErrorMessage = "Website cannot exceed 300 characters.")]
        public string Website { get; set; }



        [Required(ErrorMessage = "Please choose ")]
        public bool Gender { get; set; } // true for Male, false for Female

        public DateTime DateOfBirth { get; set; }


        [NotMapped]
        public List<EnumCommon.Languagelist> Languagelist { get; set; } = new List<EnumCommon.Languagelist>();

        public string LanguagelistSerialized
        {
            get => Languagelist != null ? string.Join(",", Languagelist) : string.Empty;
            set => Languagelist = value?.Split(',')
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => (EnumCommon.Languagelist)Enum.Parse(typeof(EnumCommon.Languagelist), v))
                .ToList() ?? new List<EnumCommon.Languagelist>();
        }


    }
}


public enum AcademicAttainment
{
    [Display(Name = "No Academic Attainment")]
    None,

    [Display(Name = "High School Diploma")]
    HighSchool,

    [Display(Name = "Bachelor's Degree")]
    Bachelor,

    [Display(Name = "Master's Degree")]
    Master,

    [Display(Name = "Doctorate Degree")]
    Doctorate,

    [Display(Name = "Post-Doctorate")]
    PostDoctorate,

    [Display(Name = "Other Academic Attainment")]
    Other
}


public enum CurrentPosition
{
    Unemployed,          // Currently Unemployed
    Student,             // Student
    EntryLevel,          // Entry Level
    Junior,              // Junior
    Intermediate,        // Intermediate
    Senior,              // Senior
    Managerial,          // Managerial
    Executive,           // Executive
    Other                // Other
}

