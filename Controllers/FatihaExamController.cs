using Azure.Core;
using Fatiha__app.Data;
using Fatiha__app.Models;
using Fatiha__app.Models.ViewModel;
using Fatiha__app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Fatiha__app.Models.EnumCommon;

namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FatihaExamController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FatihaExamController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admins,Instructors")]
        public async Task<IActionResult> GetExams([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var totalItems = await _context.fatihaExams.CountAsync();
            var items = await _context.fatihaExams
                    .Include(f => f.language) // ✅ تضمين العلاقة

                .Select(f => new
                {
                    f.Id,
                    f.Question,
                    f.Answer1,
                    f.Answer2,
                    f.Answer3,
                    f.Answer4,
                    f.CorrectAnswer,
                    LanguageName = ((Languagelist)f.language.Id).ToString() // ✅ استخدام `f.Language.Id`
                })

                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new
            {
                FatihaExams = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Ok(model);
        }

        // GET: api/FatihaExam/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExam(int id)
        {
            var exam = await _context.fatihaExams
                .Include(f => f.language)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (exam == null)
            {
                return NotFound();
            }

            // إعادة تفاصيل السؤال في صيغة JSON
            var result = new
            {
                exam.Question,
                exam.Answer1,
                exam.Answer2,
                exam.Answer3,
                exam.Answer4,
                exam.CorrectAnswer
            };

            return Ok(result);
        }

        // POST: api/FatihaExam
        [HttpPost]
        [Authorize(Roles = "Admins")]
        public async Task<IActionResult> CreateExam([FromBody] FatihaExam exam)
        {
            if (exam == null)
                return BadRequest("Invalid exam data.");

            _context.fatihaExams.Add(exam);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExam), new { id = exam.Id }, exam);
        }

        // PUT: api/FatihaExam/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admins")]
        public async Task<IActionResult> UpdateExam(int id, [FromBody] FatihaExam exam)
        {
            if (id != exam.Id)
                return BadRequest("Exam ID mismatch.");

            var existingExam = await _context.fatihaExams.FindAsync(id);
            if (existingExam == null)
                return NotFound();

            // تحديث الخصائص (يمكن استخدام AutoMapper للتبسيط)
            existingExam.Question = exam.Question;
            existingExam.Answer1 = exam.Answer1;
            existingExam.Answer2 = exam.Answer2;
            existingExam.Answer3 = exam.Answer3;
            existingExam.Answer4 = exam.Answer4;
            existingExam.CorrectAnswer = exam.CorrectAnswer;
            existingExam.languageId = exam.languageId; // تأكد من أن الخاصية موجودة

            _context.fatihaExams.Update(existingExam);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/FatihaExam/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admins")]
        public async Task<IActionResult> DeleteExam(int id)
        {
            var exam = await _context.fatihaExams.FindAsync(id);
            if (exam == null)
                return NotFound();

            _context.fatihaExams.Remove(exam);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/FatihaExam/StartTest?FatihaRequestId=123&languageId=456
        [HttpGet("StartTest")]
        [Authorize]
        public async Task<IActionResult> StartTest([FromQuery] int? FatihaRequestId, [FromQuery] int? languageId)
        {
            var languages = await _context.languages.ToListAsync();
            var questions = new List<FatihaExam>();

            if (languageId.HasValue)
            {
                questions = await _context.fatihaExams
                    .Where(f => f.languageId == languageId)
                    .Include(f => f.language)
                    .OrderBy(r => Guid.NewGuid())
                    .Take(5)
                    .ToListAsync();
            }

            var viewModel = new TestViewModel
            {
                Languages = languages,
                Questions = questions
            };

            return Ok(new { TestData = viewModel, FatihaRequestId });
        }

        // POST: api/FatihaExam/SubmitTest
        [HttpPost("SubmitTest")]
        [Authorize]
        public async Task<IActionResult> SubmitTest([FromBody] TestSubmission submission)
        {
            if (submission == null || submission.SubmittedAnswers == null || !submission.SubmittedAnswers.Any())
            {
                return BadRequest("No answers provided.");
            }

            // جلب الأسئلة الفعلية مع الإجابات الصحيحة
            var questionIds = submission.SubmittedAnswers.Select(q => q.Id).ToList();
            var actualQuestions = await _context.fatihaExams
                .Where(q => questionIds.Contains(q.Id))
                .ToListAsync();

            int correctAnswersCount = 0;
            foreach (var submittedAnswer in submission.SubmittedAnswers)
            {
                var actualQuestion = actualQuestions.FirstOrDefault(q => q.Id == submittedAnswer.Id);
                if (actualQuestion != null && actualQuestion.CorrectAnswer == submittedAnswer.CorrectAnswer)
                {
                    correctAnswersCount++;
                }
            }

            double percentage = (double)correctAnswersCount / 5 * 100; // نفترض 5 أسئلة
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRequests = await _context.fatihaRequests
                .Where(fr => fr.ApplicationUserId == userId)
                .ToListAsync();

            if (userRequests != null && userRequests.Any() && percentage > 59)
            {
                foreach (var userRequest in userRequests)
                {
                    userRequest.Status = FatihaRequestStatus.Qualified;
                    _context.Update(userRequest);
                }
                await _context.SaveChangesAsync();
            }

            var result = new
            {
                Percentage = percentage,
                CorrectAnswers = correctAnswersCount,
                TotalQuestions = 5,
                FatihaRequestId = submission.FatihaRequestId
            };

            return Ok(result);
        }
    }

    // DTO لتعريف بيانات تسليم الاختبار
    public class TestSubmission
    {
        public List<FatihaExam> SubmittedAnswers { get; set; }
        public int FatihaRequestId { get; set; }
    }
}
