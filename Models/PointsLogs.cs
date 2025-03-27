using Fatiha__app.Models;
using System.ComponentModel.DataAnnotations;

public class PointsLogs
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } // تأكد من تطابق الاسم

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfRecord { get; set; }

    [Required]
    public int Points { get; set; }

    [Required]
    public int FatihaRequestId { get; set; } // يجب أن يكون من نوع int
    public virtual FatihaRequest FatihaRequest { get; set; }
}