using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fatiha__app.Models
{
    public class FatihaExam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Question { get; set; }

        [Required]
        [StringLength(200)]
        public string Answer1 { get; set; }

        [Required]
        [StringLength(200)]
        public string Answer2 { get; set; }

        [Required]
        [StringLength(200)]
        public string Answer3 { get; set; }

        [Required]
        [StringLength(200)]
        public string Answer4 { get; set; }

        [Range(1, 4)]
        public int CorrectAnswer { get; set; }

        [ForeignKey("languageId")]
        public int languageId { get; set; }

        public languageS language { get; set; }

    }
}
