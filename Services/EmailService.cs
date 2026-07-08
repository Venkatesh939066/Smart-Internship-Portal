using Resend;

namespace SmartInternshipPortal.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;

        public EmailService(IResend resend)
        {
            _resend = resend;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string html)
        {
            var message = new EmailMessage
            {
                From = "Smart Internship Portal <onboarding@resend.dev>",
                Subject = subject,
                HtmlBody = html
            };

            message.To.Add(to);

            await _resend.EmailSendAsync(message);
        }
    }
}