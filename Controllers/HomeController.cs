using Fatiha__app.Data;
using Fatiha__app.Models;
using Fatiha__app.Models.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Home/Index
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            // الحصول على معرّف المستخدم الحالي
            var userId = _userManager.GetUserId(User);

            // جلب أول FatihaRequest للمستخدم الحالي
            var request = await _context.fatihaRequests
                                        .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            // إنشاء نموذج بيانات HomeVM
            var viewModel = new HomeVM
            {
                Request = request,
                Numberofstudent = await _userManager.Users.CountAsync(),
                Numberofmjazin = await _context.AuthorizedUsers.CountAsync(),
                Numberofcertificates = await _context.certificates.CountAsync()
                // يمكن إضافة بيانات أخرى حسب الحاجة
            };

            return Ok(viewModel);
        }

        // GET: api/Home/Admin
        [HttpGet("Admin")]
        public IActionResult Admin()
        {
            var countData = new countVM
            {
                numofAlqerats = _context.alQerats.Count(),
                numofAuthroized = _context.AuthorizedUsers.Count(),
                numofExam = _context.fatihaExams.Count(),
                numofRequests = _context.fatihaRequests.Count()
            };

            return Ok(countData);
        }

        // GET: api/Home/Privacy
        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            // يمكنك إعادة سياسة الخصوصية كنص أو بيانات JSON
            return Ok(new { Message = "Privacy policy information." });
        }

        // GET: api/Home/Error
        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return StatusCode(500, errorModel);
        }
    }
}
