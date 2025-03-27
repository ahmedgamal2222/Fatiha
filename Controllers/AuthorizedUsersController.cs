using Fatiha__app.Data;
using Fatiha__app.Models;
using Fatiha__app.Models.ViewModel;
using Fatiha__app.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizedUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly FileUploadService _fileUploadService;
        private readonly DigitalOceanSpaceUploaderService _uploaderService;

        public AuthorizedUsersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            DigitalOceanSpaceUploaderService uploaderService,
            FileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _uploaderService = uploaderService;
            _fileUploadService = fileUploadService;
        }

        // GET: api/AuthorizedUsers?pageNumber=1
        [HttpGet]
        public IActionResult GetAuthorizedUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 5;

            var totalRecords = _context.AuthorizedUsers.Count();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var authorizedUsers = _context.AuthorizedUsers
                .Include(u => u.ApplicationUser)
                .OrderBy(u => u.Id) // مهم جداً علشان يضمن ترتيب البيانات
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                authorizedUsers,
                pageNumber,
                totalPages
            });
        }


        // GET: api/AuthorizedUsers/user
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserAuthorizedUsers()
        {
            string currentUserId = _userManager.GetUserId(User);
            var authorizedUsers = await _context.AuthorizedUsers
                .Where(u => u.ApplicationUserId == currentUserId)
                .ToListAsync();

            return Ok(authorizedUsers);
        }
        [HttpGet("username/{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserNameById(string id)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            return Ok(new { userName = user.UserName }); // ✅ إرجاع `UserName`
        }



        // GET: api/AuthorizedUsers/search?username=someusername
        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> SearchByUserName([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username cannot be null or empty");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var authorizedUser = await _context.AuthorizedUsers
                .Include(a => a.ApplicationUser)
                .Where(a => a.ApplicationUserId == user.Id)
                .ToListAsync();

            return Ok(authorizedUser);
        }

        // POST: api/AuthorizedUsers/authorize?userId=xxx&alQeratId=yyy
        [HttpPost("authorize")]
        [Authorize]
        public async Task<IActionResult> Authorize([FromQuery] string userId, [FromQuery] int alQeratId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("User not found.");
            }
            bool isAuthorized = await _context.AuthorizedUsers
    .AnyAsync(au => au.ApplicationUserId == currentUser.Id
        && au.AlQeratId == alQeratId
        && au.DateOfAuthorization != null);

            if (!isAuthorized)
            {
                return Forbid("You are not authorized to approve this Qerat.");
            }

            var authorizedUser = _context.AuthorizedUsers
                .FirstOrDefault(a => a.ApplicationUserId == userId && a.AlQeratId == alQeratId);
            if (authorizedUser == null)
            {
                return NotFound("Authorization record not found.");
            }

            if (authorizedUser.ApplicationUser == null)
            {
                authorizedUser.ApplicationUserId = userId;
            }

            authorizedUser.AuthorizedByUserId = currentUser.Id;
            authorizedUser.DateOfAuthorization = DateTime.Now;

            _context.Update(authorizedUser);

            var user = await _userManager.FindByIdAsync(authorizedUser.ApplicationUserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }


            var addToRoleResult = await _userManager.AddToRoleAsync(user, "Instructors");
            if (!addToRoleResult.Succeeded)
            {
                return BadRequest("This user is already authorized");
            }

            await _context.SaveChangesAsync();
            return Ok("User authorized successfully");
        }

        // GET: api/AuthorizedUsers/apply
        [HttpGet("apply")]
        [Authorize]
        public async Task<IActionResult> GetApplyData()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("User not found.");
            }

            var userApplications = _context.AuthorizedUsers
                .Where(a => a.ApplicationUserId == currentUser.Id).ToList();

            var userAppliedQeratIds = userApplications.Select(a => a.AlQeratId).ToList();

            var qerat = _context.alQerats
                .Where(q => !userAppliedQeratIds.Contains(q.Id)).ToList();

            return Ok(new { QeratList = qerat });
        }

        [HttpPost("apply")]
        [Authorize]

        public async Task<IActionResult> SubmitApplication([FromForm] SubmitApplicationVM model)
        {
            if (string.IsNullOrEmpty(model.AuthorizedUser))
            {
                return BadRequest("Invalid request: AuthorizedUser is null.");
            }

            var authorizedUser = JsonConvert.DeserializeObject<AuthorizedUsers>(model.AuthorizedUser);

            if (authorizedUser == null)
            {
                return BadRequest("Invalid request: Could not deserialize AuthorizedUser.");
            }

            if (model.AudioFile != null)
            {
                using (var stream = model.AudioFile.OpenReadStream())
                {
                    string fileName = Guid.NewGuid().ToString() + ".wav";
                    var uploadedUrl = await _uploaderService.UploadFileAsync(stream, fileName, model.AudioFile.ContentType);
                    authorizedUser.AudioRecordingRequest = uploadedUrl;
                }
            }

            if (model.CvFile != null)
            {
                var cvUploadResult = await _fileUploadService.UploadFileAsync(model.CvFile, "authorizedusers");
                if (!string.IsNullOrEmpty(cvUploadResult))
                {
                    authorizedUser.Cv = cvUploadResult;
                }
            }
            if (model.ImgFile != null)
            {
                var imgUploadResult = await _fileUploadService.UploadFileAsync(model.ImgFile, "authorizedusers");
                if (!string.IsNullOrEmpty(imgUploadResult))
                {
                    authorizedUser.ProfileImg = imgUploadResult;
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("User not found.");
            }
            authorizedUser.ApplicationUserId = currentUser.Id;
            authorizedUser.DateOfRequest = DateTime.Now;

            _context.AuthorizedUsers.Add(authorizedUser);
            await _context.SaveChangesAsync();
            if (authorizedUser.Languagelist == null)
            {
                authorizedUser.Languagelist = new List<EnumCommon.Languagelist>();
            }


            return Ok("Request sent successfully");
        }

        // 📌 دالة مساعدة لرفع الملفات
        private async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;

            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            return await _fileUploadService.UploadFileAsync(file, $"authorizedusers/{folder}");
        }

        // GET: api/AuthorizedUsers/{id?}
        // GET: api/AuthorizedUsers/{id?}
        [HttpGet("{id?}")] // جعل id اختياريًا
        [Authorize]
        public IActionResult GetDetails(string? id)
        {
            var userId = User.FindFirst("nameid")?.Value; // استخراج userId من التوكن

            if (string.IsNullOrEmpty(id))
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is missing.");
                }

                // جلب بيانات المستخدم بناءً على userId (كـ GUID)
                var userApplication = _context.AuthorizedUsers
                    .Include(a => a.AlQerats)
                    .Include(u => u.ApplicationUser)
                    .FirstOrDefault(a => a.ApplicationUserId == userId);

                if (userApplication == null)
                {
                    return NotFound();
                }

                return Ok(userApplication);
            }

            // التحقق إذا كان id رقم أم لا
            if (int.TryParse(id, out int numericId))
            {
                // البحث باستخدام Id (رقم)
                var applicationById = _context.AuthorizedUsers
                    .Include(a => a.AlQerats)
                    .Include(u => u.ApplicationUser)
                    .FirstOrDefault(a => a.Id == numericId);

                if (applicationById != null)
                {
                    return Ok(applicationById);
                }
            }
            else
            {
                // البحث باستخدام ApplicationUserId (كـ string)
                var applicationByUserId = _context.AuthorizedUsers
                    .Include(a => a.AlQerats)
                    .Include(u => u.ApplicationUser)
                    .FirstOrDefault(a => a.ApplicationUserId == id);

                if (applicationByUserId != null)
                {
                    return Ok(applicationByUserId);
                }
            }

            return NotFound();
        }


        // DELETE: api/AuthorizedUsers/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admins")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var authorizedUser = await _context.AuthorizedUsers.FindAsync(id);
            if (authorizedUser == null)
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(authorizedUser.ApplicationUserId);
            try
            {
                _context.AuthorizedUsers.Remove(authorizedUser);
                if (user != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, "Instructors");
                }
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Can't delete this authorized user because it's related to other records. Error: {ex.Message}");
            }
        }
        [HttpGet("instructor")]
        [Authorize(Roles = "Instructors")]
        public async Task<IActionResult> GetInstructorAuthorizedUsers(
     [FromQuery] string userId, // استقبال userId بدلاً من instructor
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 5)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required.");
            }

            var currentUser = await _userManager.FindByIdAsync(userId); // البحث عن المستخدم باستخدام userId
            if (currentUser == null)
            {
                return NotFound("User not found.");
            }

            // الحصول على الأدوار المرتبطة بالمستخدم
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            if (!userRoles.Contains("Instructors"))
            {
                return Forbid("User does not have the required role.");
            }

            // الحصول على القرات التي تم تجييز المستخدم لها
            var authorizedQeratIds = await _context.AuthorizedUsers
                .Where(au => au.ApplicationUserId == currentUser.Id && au.DateOfAuthorization != null)
                .Select(au => au.AlQeratId)
                .ToListAsync();

            if (!authorizedQeratIds.Any())
            {
                return Ok(new { authorizedUsers = new List<AuthorizedUsers>(), pageNumber, totalPages = 0 });
            }

            // استعلام لطلبات الإجازة التي تخص القرات المصرح بها ولم تتم الموافقة عليها بعد
            var query = _context.AuthorizedUsers
                .Include(u => u.ApplicationUser)
                .Where(au => authorizedQeratIds.Contains(au.AlQeratId) && au.DateOfAuthorization == null);

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var authorizedUsers = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                authorizedUsers,
                pageNumber,
                totalPages
            });
        }
    }
}
