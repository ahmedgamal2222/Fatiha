using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Fatiha__app.Data;
using Fatiha__app.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using FirebaseAdmin.Auth;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly string _sendGridApiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;
        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 IConfiguration configuration,
                                 IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _sendGridApiKey = configuration["EmailSettings:SendGridApiKey"];
            _senderEmail = configuration["EmailSettings:SenderEmail"];
            _senderName = configuration["EmailSettings:SenderName"];

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email)
    };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenExpiration = model.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(8);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = tokenExpiration,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // ✅ حفظ التوكن في جدول AspNetUserTokens
            await _userManager.SetAuthenticationTokenAsync(user, "FatihaApp", "JWT", tokenString);

            // ✅ إرسال التوكن كـ كوكي `HttpOnly` و `Secure`
            Response.Cookies.Append("authToken", tokenString, new CookieOptions
            {
                HttpOnly = true,  // 🔒 يمنع الوصول إلى الكوكي من JavaScript
                Secure = true,    // 🔒 يمنع الإرسال عبر HTTP (يجب أن يكون الموقع HTTPS)
                SameSite = SameSiteMode.Strict, // 🔒 يمنع CSRF
                Expires = tokenExpiration // ⏳ ينتهي بناءً على `RememberMe`
            });

            // ✅ إرجاع التوكن في الاستجابة أيضًا
            return Ok(new { token = tokenString, message = "Login successful" });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "User not found in our database." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"https://www.fatiha.id/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(model.Email)}";

            await _emailSender.SendEmailAsync(model.Email, "Password Reset Request", $"Please click <a href='{resetLink}'>here</a> to reset your password.");

            return Ok(new { message = "Password reset email sent successfully." });
        }



        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "المستخدم غير موجود." });
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح." });
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }


        public class ForgotPasswordModel
        {
            public string Email { get; set; }
        }


        // POST: api/Account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { message = "Passwords do not match" });
            }

            var user = new IdentityUser
            {
                Email = model.Email,
                UserName = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return Ok(new { message = "User registered successfully" });
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("authToken"); // ✅ حذف الكوكي عند تسجيل الخروج
            await _signInManager.SignOutAsync();
            return Ok(new { message = "User logged out successfully" });
        }


        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || user.EmailConfirmed)
            {
                return BadRequest(new { message = "User not found or already confirmed." });
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"https://yourapp.com/confirm-email?email={model.Email}&token={token}";
            await _emailSender.SendEmailAsync(model.Email, "Confirm Your Email", $"Click <a href='{confirmationLink}'>here</a> to confirm your email.");

            return Ok(new { message = "Confirmation email sent." });
        }
    }


    public class ForgotPasswordModel
    {
        public string Email { get; set; }
    }

    public class ResendConfirmationModel
    {
        public string Email { get; set; }
    }

}


public class ResetPasswordModel
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}

// نموذج بيانات تسجيل الدخول
public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; } // ✅ إضافة Remember Me

}

// نموذج بيانات التسجيل
public class RegisterModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}


