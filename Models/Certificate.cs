using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fatiha__app.Models
{
    public class Certificate
    {
        public int Id { get; set; }

       
        [Required]
        public string AuthorizedByUserId { get; set; }
        public IdentityUser AuthorizedByUser { get; set; }  // Navigation property for AuthorizedByUserId

        [Required]
        public string ApplicationUserId { get; set; }
        public IdentityUser ApplicationUser { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfIssue { get; set; }
        public byte[] PdfData { get; set; }



        // Relationships
        public int FatihaRequestId { get; set; }  // Changed type to int
        public FatihaRequest FatihaRequest { get; set; }
        // Constructors, methods, and other members as needed
    }
}
