using SendGrid.Helpers.Mail;
using SendGrid;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Fatiha__app.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string _sendGridApiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailSender(IConfiguration configuration)
        {
            _sendGridApiKey = configuration["EmailSettings:SendGridApiKey"];
            _senderEmail = configuration["EmailSettings:SenderEmail"];
            _senderName = configuration["EmailSettings:SenderName"];
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlMessage, htmlMessage);
            await client.SendEmailAsync(msg);
        }
    }

}
