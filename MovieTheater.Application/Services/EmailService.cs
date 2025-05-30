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
      <p style=""margin-top:30px;"">Best regards,<br/>MovieTheater Team</p>
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
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater Logo"" style=""max-width:150px;height:auto;"">
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

        public async Task SendForgotPasswordOtpEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#000000;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#000000;padding:20px;background-color:#000000;"">
    <div style=""max-width:600px;margin:auto;background:#ffffff;border:1px solid #f8c439;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater Logo"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#f8c439;font-size:22px;text-align:center;"">Reset Your Password</h1>
      <p>We received a request to reset your password for your MovieTheater account. Please use the following code to proceed with password reset:</p>
      <div style=""background-color:#f8f8f8;padding:15px;border-radius:5px;text-align:center;margin:20px 0;font-size:24px;font-weight:bold;letter-spacing:5px;border:2px solid #f8c439;"">
        {request.Otp}
      </div>
      <p>This code will expire in 15 minutes. If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
      <p style=""margin-top:30px;"">Best regards,<br/>MovieTheater Team</p>
    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "OTP password recovery at MovieTheater", html);
        }

        public async Task SendPasswordChangeEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#000000;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#000000;padding:20px;background-color:#000000;"">
    <div style=""max-width:600px;margin:auto;background:#ffffff;border:1px solid #f8c439;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater Logo"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#f8c439;font-size:22px;"">Password Reset Successful!</h1>
      <p>Hello {request.UserName},</p>
      <p>Your password has been successfully reset for your MovieTheater account.</p>
      <p>You can now log in with your new password and continue booking tickets for your favorite movies.</p>
      <div style=""text-align:center;margin:25px 0;"">
        <a href=""https://movietheater.ae-tao-fullstack-api.com/login"" style=""background-color:#f8c439;color:#000000;padding:10px 20px;text-decoration:none;border-radius:4px;font-weight:bold;"">Login Now</a>
      </div>
      <p>If you didn't make this change or if you have any concerns about your account security, please contact our support team immediately.</p>
      <p style=""margin-top:30px;"">Best regards,<br/>MovieTheater Team</p>
    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "Password has been changed at MovieTheater", html);
        }

        public async Task SendEmployeeCredentialsEmailAsync(EmployeeCredentialsEmailDto request)
        {
            var html = $@"
<html style=""background-color:#000000;margin:0;padding:0;"">
  <body style=""font-family:Arial,sans-serif;color:#000000;padding:20px;background-color:#000000;"">
    <div style=""max-width:600px;margin:auto;background:#ffffff;border:1px solid #f8c439;border-radius:6px;padding:20px;"">
      <div style=""text-align:center;margin-bottom:20px;"">
        <img src=""https://placeholder.com/logo.png"" alt=""MovieTheater Logo"" style=""max-width:150px;height:auto;"">
      </div>
      <h1 style=""color:#f8c439;font-size:22px;"">Welcome {request.UserName}!</h1>
      <p>Your account has been created successfully.</p>
      <p>Here are your login credentials:</p>
      <ul>
        <li><strong>Email:</strong> {request.To}</li>
        <li><strong>Password:</strong> {request.Password}</li>
      </ul>
      <p>Please change your password after your first login to keep your account secure.</p>
      <div style=""text-align:center;margin:25px 0;"">
        <a href=""https://placeholder.com/login"" style=""background-color:#f8c439;color:#000000;padding:10px 20px;text-decoration:none;border-radius:4px;font-weight:bold;"">Login Now</a>
      </div>
      <p style=""margin-top:30px;"">Best regards,<br/>MovieTheater Team</p>
    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "Your Account Credentials", html);
        }
    }
}