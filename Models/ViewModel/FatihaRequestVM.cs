using System.ComponentModel.DataAnnotations;

public class FatihaRequestVM
{
    public int? Id { get; set; }

    [Required]
    public int AlQeratId { get; set; }

    public string? AudioRecord { get; set; }

    [Required]
    public DateTime DateOfRecord { get; set; }

    [Required]
    [DataType(DataType.Upload)]
    public IFormFile? AudioFile { get; set; }

    [Required]
    [StringLength(500, ErrorMessage = "RequestLetter cannot exceed 500 characters.")]
    public string RequestLetter { get; set; } // تم إضافتها
}
