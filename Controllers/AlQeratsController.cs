using Fatiha__app.Data;
using Fatiha__app.Models;
using Fatiha__app.Models.ViewModel;
using Fatiha__app.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fatiha__app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlQeratsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AlQeratsController(ApplicationDbContext context, FileUploadService fileUploadService, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _hostEnvironment = hostEnvironment;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll(int? pageNumber = null, int? pageSize = null)
        {
            var alQerats = _context.alQerats.AsQueryable();

            // لو مفيش ترقيم، رجّع كل البيانات
            if (pageNumber == null || pageSize == null || pageSize == 0)
            {
                var allItems = await alQerats.ToListAsync();
                return Ok(new { AlQerats = allItems });
            }

            // حساب عدد الصفحات لو تم تمرير القيم
            var totalItems = await alQerats.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await alQerats
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToListAsync();

            var viewModel = new
            {
                AlQerats = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return Ok(viewModel);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var alQerat = await _context.alQerats.FindAsync(id);
            if (alQerat == null)
                return NotFound();
            return Ok(alQerat);
        }

        [Authorize(Roles = "Admins")]
        [HttpPost]
        [Consumes("multipart/form-data")] // حل مشكلة Swagger مع IFormFile
        public async Task<IActionResult> Create([FromForm] AlQeratCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newAlQerat = new AlQerat
            {
                QeratName = model.QeratName,
                Description = model.Description
            };

            if (model.AudioFile != null)
                newAlQerat.AudioFile = await _fileUploadService.UploadFileAsync(model.AudioFile, "alqerats");

            _context.alQerats.Add(newAlQerat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = newAlQerat.Id }, newAlQerat);
        }

        [Authorize(Roles = "Admins")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")] // حل مشكلة Swagger مع IFormFile
        public async Task<IActionResult> Edit(int id, [FromForm] AlQeratCreateViewModel model)
        {
            var existingAlQerat = await _context.alQerats.FindAsync(id);
            if (existingAlQerat == null)
                return NotFound();

            existingAlQerat.QeratName = model.QeratName;
            existingAlQerat.Description = model.Description;

            if (model.AudioFile != null)
            {
                if (!string.IsNullOrWhiteSpace(existingAlQerat.AudioFile))
                {
                    var oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, existingAlQerat.AudioFile);
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                existingAlQerat.AudioFile = await _fileUploadService.UploadFileAsync(model.AudioFile, "alqerats");
            }

            _context.Update(existingAlQerat);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Admins")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var alQerat = await _context.alQerats.FindAsync(id);
            if (alQerat == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(alQerat.AudioFile))
            {
                var filePath = Path.Combine(_hostEnvironment.WebRootPath, alQerat.AudioFile);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.alQerats.Remove(alQerat);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
