using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Core;
using Fatiha__app.Data;
using Fatiha__app.Models;
using Fatiha__app.Models.ViewModel;
using Fatiha__app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FatihaRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly DigitalOceanSpaceUploaderService _uploaderService;
        private readonly UserManager<IdentityUser> _userManager;

        public FatihaRequestController(ApplicationDbContext context, IWebHostEnvironment env, DigitalOceanSpaceUploaderService uploaderService, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _env = env;
            _uploaderService = uploaderService;
            _userManager = userManager;
        }

        // GET: api/FatihaRequest?page=1
        [HttpGet]
        [Authorize(Roles = "Admins,Instructors")]
        public async Task<IActionResult> GetRequests([FromQuery] int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            var isUserAdmin = await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(userId), "Admins");

            IQueryable<FatihaRequest> query;
            if (isUserAdmin)
            {
                query = _context.fatihaRequests
                         .Include(r => r.ApplicationUser)
                         .Include(r => r.AlQerat)
                         .OrderBy(r => r.DateOfRecord);
            }
            else
            {
                var authorizedUser = await _context.AuthorizedUsers
                                         .Include(u => u.AlQerats)
                                         .FirstOrDefaultAsync(u => u.ApplicationUserId == userId);
                if (authorizedUser == null)
                {
                    return NotFound("Authorized user record not found.");
                }
                query = _context.fatihaRequests
                         .Include(r => r.ApplicationUser)
                         .Include(r => r.AlQerat)
                         .Where(r => r.AlQeratId == authorizedUser.AlQeratId)
                         .OrderBy(r => r.DateOfRecord);
            }

            var sortedRequests = await query.ToListAsync();
            int pageSize = 5;
            var pagedRequests = sortedRequests.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)sortedRequests.Count / pageSize);

            return Ok(new { PageNumber = page, TotalPages = totalPages, Requests = pagedRequests });
        }

        // GET: api/FatihaRequest/user
        [HttpGet("user")]
        [Authorize]
        public IActionResult GetUserRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRequests = _context.fatihaRequests
                .Include(r => r.ApplicationUser)
                .Include(r => r.AlQerat)
                .Where(r => r.ApplicationUser.Id == userId)
                .OrderBy(r => r.DateOfRecord)
                .ToList();

            return Ok(userRequests);
        }

        // GET: api/FatihaRequest/search?username=someusername
        [HttpGet("search")]
        public async Task<IActionResult> SearchByUser([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username cannot be empty.");
            }

            var results = await _context.fatihaRequests
                                    .Include(fr => fr.ApplicationUser)
                                    .Where(fr => fr.ApplicationUser.UserName == username)
                                    .ToListAsync();

            return Ok(results);
        }

        // GET: api/FatihaRequest/createData
        // يوفر بيانات لإنشاء طلب جديد (مثل قائمة Qerat)
        [HttpGet("createData")]
        [Authorize]
        public IActionResult GetCreateData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRequests = _context.fatihaRequests.Where(fr => fr.ApplicationUserId == userId).ToList();
            var userQeratIds = userRequests.Select(r => r.AlQeratId).Where(id => id.HasValue).Select(id => id.Value).ToList();
            var qerat = _context.alQerats.Where(q => !userQeratIds.Contains(q.Id)).ToList();

            return Ok(new { QeratList = qerat });
        }
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")] // لجعل Swagger يدعم رفع الملفات
        public async Task<IActionResult> CreateRequest([FromForm] FatihaRequestVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // التحقق من وجود طلب مسبق للمستخدم لنفس القراءة
            var existingRequest = await _context.fatihaRequests
                .FirstOrDefaultAsync(fr => fr.ApplicationUserId == userId && fr.AlQeratId == model.AlQeratId);

            if (existingRequest != null)
            {
                return BadRequest("You already have an outgoing request for this Qerat. Please change the Qerat name.");
            }

            if (model.AudioFile == null)
            {
                return BadRequest("Audio file is required.");
            }

            string uploadedUrl;
            try
            {
                using (var stream = model.AudioFile.OpenReadStream())
                {
                    string fileName = $"{Guid.NewGuid()}.wav";
                    uploadedUrl = await _uploaderService.UploadFileAsync(stream, fileName, model.AudioFile.ContentType);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }

            // إنشاء كائن `FatihaRequest` وحفظ البيانات
            var fatihaRequest = new FatihaRequest
            {
                ApplicationUserId = userId,
                AlQeratId = model.AlQeratId,
                DateOfRecord = model.DateOfRecord,
                AudioRecord = uploadedUrl,
                RequestLetter = model.RequestLetter,
                Status = FatihaRequestStatus.Open // تحديد الحالة الافتراضية
            };

            _context.fatihaRequests.Add(fatihaRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequestDetails), new { id = fatihaRequest.Id }, fatihaRequest);
        }

        // GET: api/FatihaRequest/{id}
        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetRequestDetails(int id)
        {
            var request = _context.fatihaRequests
                           .Include(r => r.ApplicationUser)
                           .Include(r => r.AlQerat)
                           .FirstOrDefault(r => r.Id == id);
            if (request == null)
            {
                return NotFound();
            }

            return Ok(request);
        }

        // PUT: api/FatihaRequest/{id}
        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")] // دعم Swagger
        public async Task<IActionResult> UpdateRequest(int id, [FromForm] FatihaRequestVM model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var originalRequest = await _context.fatihaRequests.FindAsync(id);

            if (originalRequest == null)
            {
                return NotFound();
            }

            if (originalRequest.ApplicationUserId != userId)
            {
                return Forbid(); // المستخدم ليس مالك الطلب
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string uploadedUrl = originalRequest.AudioRecord;

            // إذا كان هناك ملف صوتي جديد، احذف القديم ورفع الجديد بطريقة غير متزامنة
            if (model.AudioFile != null)
            {
                var deleteTask = Task.Run(async () =>
                {
                    if (!string.IsNullOrEmpty(originalRequest.AudioRecord))
                    {
                        await _uploaderService.DeleteFileAsync(originalRequest.AudioRecord);
                    }
                });

                using (var stream = model.AudioFile.OpenReadStream())
                {
                    string fileName = $"{Guid.NewGuid()}.wav";
                    uploadedUrl = await _uploaderService.UploadFileAsync(stream, fileName, model.AudioFile.ContentType);
                }

                await deleteTask; // انتظر انتهاء الحذف بعد رفع الملف الجديد
            }

            // تحديث بيانات الطلب
            originalRequest.AlQeratId = model.AlQeratId;
            originalRequest.DateOfRecord = model.DateOfRecord != default ? model.DateOfRecord : originalRequest.DateOfRecord;
            originalRequest.AudioRecord = uploadedUrl;
            originalRequest.RequestLetter = model.RequestLetter;

            _context.Entry(originalRequest).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(originalRequest);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admins")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            Console.WriteLine($"Attempting to delete request ID: {id}");

            var fatihaRequest = await _context.fatihaRequests
                .Include(r => r.PointsLogs) // تضمين السجلات المرتبطة
                .FirstOrDefaultAsync(r => r.Id == id);

            if (fatihaRequest == null)
            {
                Console.WriteLine("Request not found in database.");
                return NotFound();
            }

            var audioRecordFileName = fatihaRequest.AudioRecord;

            try
            {
                // حذف جميع السجلات المرتبطة في PointsLogs أولًا
                if (fatihaRequest.PointsLogs != null && fatihaRequest.PointsLogs.Any())
                {
                    _context.PointsLogs.RemoveRange(fatihaRequest.PointsLogs);
                }

                // حذف الملف الصوتي
                await _uploaderService.DeleteFileAsync(audioRecordFileName);

                // حذف الطلب بعد حذف السجلات المرتبطة
                _context.fatihaRequests.Remove(fatihaRequest);
                await _context.SaveChangesAsync();

                Console.WriteLine("Request deleted successfully.");
                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine("Database error: " + dbEx.InnerException?.Message);
                return StatusCode(500, "Database error: " + dbEx.InnerException?.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error: " + ex.Message);
                return StatusCode(500, "Unexpected error: " + ex.Message);
            }
        }



        // GET: api/FatihaRequest/{id}/details
        [HttpGet("{id}/details")]
        [Authorize]
        public async Task<IActionResult> GetRequestFullDetails(int id)
        {
            var comments = await _context.fatihaRequestsComment
                                .Where(c => c.FatihaRequestId == id)
                                .Include(x => x.ApplicationUser)
                                .ToListAsync();

            var request = await _context.fatihaRequests
                                .Include(r => r.ApplicationUser)
                                .Include(r => r.AlQerat)
                                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admins") && !User.IsInRole("Instructors"))
            {
                if (request.ApplicationUserId != userId)
                {
                    return Unauthorized();
                }
            }

            bool adminOrInstructorCommentExists = _context.fatihaRequestsComment
                .Where(fc => fc.FatihaRequestId == id && (User.IsInRole("Admins") || User.IsInRole("Instructors")))
                .Any();

            var viewModel = new FatihaRequestDetailsViewModel
            {
                FatihaRequest = request,
                CommentsList = comments,
                IsAdminsComment = adminOrInstructorCommentExists,
                FatihaRequestComment = new FatihaRequestComment()
            };

            return Ok(viewModel);
        }

        // POST: api/FatihaRequest/approve/{id}
        [HttpPost("approve/{id}")]
        [Authorize(Roles = "Admins,Instructors")]
        public IActionResult ApproveRequest(int id)
        {
            var fatihaRequest = _context.fatihaRequests.Find(id);
            if (fatihaRequest == null)
            {
                return NotFound();
            }
            fatihaRequest.DateOfAccreditation = DateTime.Now;
            fatihaRequest.IsApproved = true;
            _context.Update(fatihaRequest);
            _context.SaveChanges();

            return Ok(fatihaRequest);
        }

        // GET: api/FatihaRequest/approved?page=1
        [HttpGet("approved")]
        [Authorize]
        public IActionResult GetApprovedRequests([FromQuery] int? page)
        {
            var approvedRequests = _context.fatihaRequests
                .Include(r => r.ApplicationUser)
                .Include(r => r.AlQerat)
                .Where(r => r.IsApproved)
                .OrderBy(r => r.DateOfRecord);

            int pageNumber = page ?? 1;
            int pageSize = 10;
            var pagedRequests = approvedRequests.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling((double)approvedRequests.Count() / pageSize);

            return Ok(new { PageNumber = pageNumber, TotalPages = totalPages, Requests = pagedRequests });
        }

        [HttpPost("ChangeStatus/{id}")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest model)
        {
            if (model == null || string.IsNullOrEmpty(model.Status))
            {
                return BadRequest("Invalid status value.");
            }

            var userId = _userManager.GetUserId(User);
            var authrizedUserId = _context.AuthorizedUsers
                .FirstOrDefault(a => a.ApplicationUserId == userId)?.Id;

            var fatihaRequest = _context.fatihaRequests
                .Include(fr => fr.ApplicationUser)
                .Include(fr => fr.AuthorizedBy)
                .ThenInclude(fr => fr.ApplicationUser)
                .FirstOrDefault(fr => fr.Id == id);

            if (fatihaRequest == null)
            {
                return NotFound("FatihaRequest not found.");
            }

            if (!Enum.TryParse<FatihaRequestStatus>(model.Status, out var newStatus))
            {
                return BadRequest("Invalid status value.");
            }

            // 🔹 شرط التحقق قبل التغيير إلى Qualified (3)
            if (newStatus == FatihaRequestStatus.Qualified && fatihaRequest.AuthorizedById == null)
            {
                return BadRequest("Cannot qualify request without an authorized user.");
            }

            var previousStatus = fatihaRequest.Status;
            fatihaRequest.Status = newStatus;
            if (authrizedUserId != null)
            {
                fatihaRequest.AuthorizedById = authrizedUserId;
            }
            _context.Update(fatihaRequest);
            await _context.SaveChangesAsync();

            // ✅ تسجيل النقاط إذا تم الموافقة والإغلاق
            if (fatihaRequest.IsApproved && newStatus == FatihaRequestStatus.Closed)
            {
                var existingLogForThisRequest = _context.PointsLogs
                    .FirstOrDefault(pl => pl.FatihaRequestId == fatihaRequest.Id);

                if (existingLogForThisRequest == null)
                {
                    var existingPointsLog = _context.PointsLogs
                        .OrderByDescending(pl => pl.DateOfRecord)
                        .FirstOrDefault(pl => pl.ApplicationUserId == fatihaRequest.ApplicationUserId);

                    if (existingPointsLog != null)
                    {
                        existingPointsLog.Points += 5;
                        _context.Update(existingPointsLog);
                    }
                    else
                    {
                        var newPointsLog = new PointsLogs
                        {
                            ApplicationUserId = fatihaRequest.ApplicationUserId,
                            DateOfRecord = DateTime.Now,
                            Points = 5,
                            FatihaRequestId = fatihaRequest.Id
                        };
                        _context.PointsLogs.Add(newPointsLog);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // ✅ إصدار الشهادة عند التأهيل (Qualified)
            if (fatihaRequest.IsApproved && newStatus == FatihaRequestStatus.Qualified)
            {
                var user = fatihaRequest.AuthorizedBy?.ApplicationUser;
                if (user == null)
                {
                    return Unauthorized("User not found.");
                }

                if (fatihaRequest.ApplicationUser == null)
                {
                    return NotFound("Associated ApplicationUser for FatihaRequest not found.");
                }

                var existingCertificate = await _context.certificates
                    .Where(c => c.ApplicationUserId == fatihaRequest.ApplicationUser.Id
                                && c.FatihaRequestId == fatihaRequest.Id)
                    .FirstOrDefaultAsync();

                if (existingCertificate == null)
                {
                    var newCertificate = new Certificate
                    {
                        AuthorizedByUserId = user?.Id ?? "Unknown",
                        FatihaRequestId = fatihaRequest?.Id ?? 0,
                        ApplicationUserId = fatihaRequest?.ApplicationUserId ?? "Unknown",
                        DateOfIssue = DateTime.Now
                    };
                    _context.Add(newCertificate);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving certificate: {ex.Message}");
                    }
                }
            }

            return Ok("Status updated successfully.");
        }

        [HttpGet("{id}/certificateData")]
        public async Task<IActionResult> GetCertificateData(int id)
        {
            var fatihaRequest = await _context.fatihaRequests
                .Include(fr => fr.AuthorizedBy) // تضمين AuthorizedBy لجلب AuthorizedByUserId
                .Include(fr => fr.ApplicationUser) // بيانات مقدم الطلب
                .Include(fr => fr.AlQerat) // جلب بيانات القراءة
                .FirstOrDefaultAsync(fr => fr.Id == id);

            if (fatihaRequest == null)
            {
                return NotFound(new { message = "FatihaRequest not found." });
            }

            // عرض قيمة AuthorizedByUserId للتحقق من وجودها
            Console.WriteLine($"AuthorizedByUserId: {fatihaRequest.AuthorizedBy?.AuthorizedByUserId}");

            string sheikhName = "Not Authorized";

            if (!string.IsNullOrEmpty(fatihaRequest.AuthorizedBy?.AuthorizedByUserId))
            {
                var sheikh = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == fatihaRequest.AuthorizedBy.AuthorizedByUserId);

                Console.WriteLine($"Found Sheikh: {sheikh?.UserName}");

                if (sheikh != null)
                {
                    sheikhName = sheikh.UserName;
                }
            }

            var certificateData = new
            {
                ApplicantName = fatihaRequest.ApplicationUser?.UserName ?? "Unknown",
                SheikhName = sheikhName, // الآن يجب أن يظهر الاسم الصحيح
                ReadingName = fatihaRequest.AlQerat?.QeratName ?? "No Reading Assigned",
                DateOfIssue = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                AlQeratId = fatihaRequest.AlQerat?.Id ?? 0
            };

            return Ok(certificateData);
        }


        [HttpGet("{requestId}/downloadCertificate")]
        [Authorize]
        public async Task<IActionResult> DownloadCertificate(int requestId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized("No user logged in.");
            }

            var certificate = await _context.certificates
                .Include(c => c.ApplicationUser)
                .Include(c => c.FatihaRequest).ThenInclude(fr => fr.AlQerat)
                .Include(c => c.AuthorizedByUser)
                .FirstOrDefaultAsync(c => c.FatihaRequestId == requestId && c.FatihaRequest.ApplicationUserId == userId);

            if (certificate == null)
            {
                return NotFound("This certificate doesn't belong to you.");
            }

            var imagePath = Path.Combine("wwwroot", "certificates", $"{certificate.Id}.jpg");
            if (!System.IO.File.Exists(imagePath))
            {
                return NotFound("Certificate image not found.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            return File(fileBytes, "image/jpeg", $"Certificate_{certificate.Id}.jpg");
        }


        // POST: api/FatihaRequest/{fatihaRequestId}/AddComment
        [HttpPost("{fatihaRequestId}/AddComment")]
        [Authorize]
        [Consumes("multipart/form-data")] // دعم رفع الملفات في Swagger
        public async Task<IActionResult> AddComment(int fatihaRequestId, [FromForm] FatihaRequestCommentVM model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdminOrInstructor = roles.Contains("Admins") || roles.Contains("Instructors");

            string uploadedUrl = null;

            if (model.AudioFile != null)
            {
                using (var stream = model.AudioFile.OpenReadStream())
                {
                    string fileName = $"{Guid.NewGuid()}.wav";
                    uploadedUrl = await _uploaderService.UploadFileAsync(stream, fileName, model.AudioFile.ContentType);
                }
            }

            // إنشاء كائن `FatihaRequestComment`
            var newComment = new FatihaRequestComment
            {
                FatihaRequestId = fatihaRequestId, // أخذ المعرف من الـ Route
                ApplicationUserId = userId,
                Comment = model.CommentText,
                AudioRecord = uploadedUrl,
                DateOfRecord = DateTime.Now
            };

            _context.fatihaRequestsComment.Add(newComment);

            if (isAdminOrInstructor)
            {
                var fatihaRequest = await _context.fatihaRequests.FindAsync(fatihaRequestId);
                if (fatihaRequest != null)
                {
                    fatihaRequest.Status = FatihaRequestStatus.Processing;
                }
            }

            await _context.SaveChangesAsync();

            return Ok("Comment added successfully.");
        }


        // DELETE: api/FatihaRequest/MarkCommentAsDeleted/{id}
        [HttpDelete("MarkCommentAsDeleted/{id}")]
        [Authorize(Roles = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCommentAsDeleted(int id)
        {
            var comment = await _context.fatihaRequestsComment.FindAsync(id);
            if (comment == null)
            {
                return NotFound("Comment not found.");
            }
            comment.isdeleted = true;
            _context.fatihaRequestsComment.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok("Comment marked as deleted.");
        }
        [HttpGet("GetComments/{fatihaRequestId}")]
        [Authorize]
        public async Task<IActionResult> GetComments(int fatihaRequestId)
        {
            // جلب الكومنتات المرتبطة بالطلب المحدد
            var comments = await _context.fatihaRequestsComment
                .Where(c => c.FatihaRequestId == fatihaRequestId && !c.isdeleted) // استبعاد الكومنتات المحذوفة
                .Select(c => new
                {
                    c.Id,
                    c.Comment,
                    c.AudioRecord,
                    c.DateOfRecord,
                    UserName = c.ApplicationUser.UserName // افترض أن لديك علاقة مع ApplicationUser
                })
                .ToListAsync();

            if (comments == null || !comments.Any())
            {
                return NotFound("No comments found for this request.");
            }

            return Ok(comments);
        }

    }
}
