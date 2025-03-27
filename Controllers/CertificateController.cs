using Fatiha__app.Data;
using Fatiha__app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Element;
namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CertificateController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Certificate/DownloadCertificate
        [HttpGet("DownloadCertificate")]
        [Authorize]
        public async Task<IActionResult> DownloadCertificate()
        {
            // الحصول على معرّف المستخدم الحالي
            var userId = _userManager.GetUserId(User);

            // استرجاع أول شهادة (Certificate) تتبع طلب "فطيحة" مرتبط بالمستخدم الحالي
            var certificate = await _context.certificates
                                .Include(c => c.FatihaRequest)
                                    .ThenInclude(fr => fr.AlQerat)
                                .FirstOrDefaultAsync(c => c.FatihaRequest.ApplicationUserId == userId);

            if (certificate == null)
            {
                return NotFound("Certificate not found");
            }

            // إرجاع الشهادة بصيغة JSON
            return Ok(certificate);
        }
        [HttpPost("generate-pdf")]
        public IActionResult GeneratePDF([FromBody] CertificateRequest request)
        {
            try
            {
                var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var certificatesFolder = Path.Combine(wwwRoot, "certificates");

                // التأكد من أن المجلد موجود
                if (!Directory.Exists(certificatesFolder))
                {
                    Directory.CreateDirectory(certificatesFolder);
                }

                var pdfFileName = $"{Guid.NewGuid()}.pdf";
                var pdfPath = Path.Combine(certificatesFolder, pdfFileName);

                using (var writer = new PdfWriter(pdfPath))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(request.ImageBase64.Split(',')[1]);
                        var imageData = iText.IO.Image.ImageDataFactory.Create(imageBytes);
                        var image = new iText.Layout.Element.Image(imageData)
                            .SetWidth(500)
                            .SetAutoScale(true);

                        document.Add(image);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing image: {ex}");
                    }


                    // إضافة بيانات الشهادة
                    document.Add(new Paragraph($"{request.Language} Certificate")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(18));
                    document.Add(new Paragraph($"Awarded to: {request.Email}")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(14));
                }

                // قراءة الملف وإرساله مباشرة إلى المتصفح للتنزيل
                var fileBytes = System.IO.File.ReadAllBytes(pdfPath);
                return File(fileBytes, "application/pdf", "Certificate.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex}");
                return StatusCode(500, $"Error generating PDF: {ex.Message}");
            }

        }

        public class CertificateRequest
        {
            public string Language { get; set; }
            public string Email { get; set; }
            public string ImageBase64 { get; set; } // استلام الصورة Base64
        }


  
    }

}