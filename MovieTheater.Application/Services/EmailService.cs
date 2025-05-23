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
<html style=""background-color:#000000;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#000000;padding:20px;background-color:#000000;"">
    <div style=""max-width:600px;margin:auto;background:#ffffff;border:1px solid #f8c439;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater Logo"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#f8c439;font-size:22px;"">Welcome {request.UserName}!</h1>
      <p>You have successfully registered an account at our Cinema Booking service.</p>
      <p>We hope you enjoy browsing and booking tickets for your favorite movies.</p>
      <div style=""text-align:center;margin:25px 0;"">
        <a href=""https://placeholder.com/logo.png"" style=""background-color:#f8c439;color:#000000;padding:10px 20px;text-decoration:none;border-radius:4px;font-weight:bold;"">Browse Movies</a>
      </div>
      <p style=""margin-top:30px;"">Best regards,<br/>Cinema Booking Team</p>
    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "Signed", html);
        }

        public async Task SendOtpVerificationEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#000000;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#000000;padding:20px;background-color:#000000;"">
    <div style=""max-width:600px;margin:auto;background:#ffffff;border:1px solid #f8c439;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://i.ibb.co/vW3Wtx4/rimberio-2.png"" alt=""MovieTheater Logo"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#f8c439;font-size:22px;text-align:center;"">Verify Your Email</h1>
      <p>Thank you for registering with our cinema booking service. Please use the following code to verify your email address:</p>
      <div style=""background-color:#f8f8f8;padding:15px;border-radius:5px;text-align:center;margin:20px 0;font-size:24px;font-weight:bold;letter-spacing:5px;border:2px solid #f8c439;"">
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
