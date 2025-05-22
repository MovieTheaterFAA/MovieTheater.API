using Microsoft.Extensions.Configuration;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.EmailDTOs;
using Resend;

namespace MovieTheater.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly IResend _resend;

        public EmailService(IResend resend, IConfiguration configuration)
        {
            _resend = resend;
            _fromEmail = configuration["RESEND_FROM"] ?? "noreply@movie-theater.com";
        }

        private async Task SendEmailAsync(string to, string subject, string htmlContent)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = subject,
                HtmlBody = htmlContent
            };

            message.To.Add(to);
            await _resend.EmailSendAsync(message);
        }

        public async Task SendRegistrationSuccessEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#ebeaea;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#252424;padding:20px;background-color:#ebeaea;"">
    <div style=""max-width:600px;margin:auto;background:#fff;border:1px solid #d02a2a;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#d02a2a;font-size:22px;"">Welcome {request.UserName}!</h1>
      <p>You have successfully registered an account at our Cinema Booking service.</p>
      <p>We hope you enjoy browsing and booking tickets for your favorite movies.</p>
      <div style=""text-align:center;margin:25px 0;"">
        <a href=""https://your-cinema-website.com/movies"" style=""background-color:#d02a2a;color:white;padding:10px 20px;text-decoration:none;border-radius:4px;font-weight:bold;"">Browse Movies</a>
      </div>
      <p style=""margin-top:30px;"">Best regards,<br/>MovieTheater Team</p>
    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "Signed", html);
        }

        public async Task SendOtpVerificationEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#ebeaea;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#252424;padding:20px;background-color:#ebeaea;"">
    <div style=""max-width:600px;margin:auto;background:#fff;border:1px solid #d02a2a;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#d02a2a;font-size:22px;text-align:center;"">Verify Your Email</h1>
      <p>Thank you for registering with our cinema booking service. Please use the following code to verify your email address:</p>
      <div style=""background-color:#f4f4f4;padding:15px;border-radius:5px;text-align:center;margin:20px 0;font-size:24px;font-weight:bold;letter-spacing:5px;"">
        {request.Otp}
      </div>
      <p>This code will expire in 10 minutes. If you didn't request this code, please ignore this email.</p>
      <p style=""margin-top:30px;"">Best regards,<br/>MovieTheater Team</p>
    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "OTP authentication at MovieTheater", html);
        }
    }
}
