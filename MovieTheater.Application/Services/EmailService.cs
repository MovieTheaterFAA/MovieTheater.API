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
<html style=""background-color:#0d0d0d;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0d0d0d;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);border:1px solid #f8c439;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(248,196,57,0.1);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <img src=""https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=logo%2Flogo.png&version_id=null"" alt=""MovieTheater Logo"" style=""max-width:180px;height:auto;filter:brightness(0) invert(1);"">
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#f8c439;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Welcome {request.UserName}!</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #f8c439, #ffd700);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:18px;margin:0 0 16px 0;text-align:center;"">You have successfully registered an account at our MovieTheater service.</p>
        <p style=""color:#b3b3b3;font-size:16px;margin:0;text-align:center;"">We hope you enjoy browsing and booking tickets for your favorite movies.</p>
      </div>

      <div style=""text-align:center;margin:40px 0;"">
        <a href=""https://movietheater.ae-tao-fullstack-api.com/login"" style=""display:inline-block;background:linear-gradient(135deg, #f8c439 0%, #ffd700 100%);color:#000000;padding:16px 32px;text-decoration:none;border-radius:12px;font-weight:bold;font-size:16px;box-shadow:0 8px 24px rgba(248,196,57,0.3);transition:all 0.3s ease;"">Browse Movies</a>
      </div>

      <div style=""border-top:1px solid #333333;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Best regards,<br/><span style=""color:#f8c439;font-weight:600;"">MovieTheater Team</span></p>
      </div>

    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "Signed", html);
        }

        public async Task SendOtpVerificationEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#0d0d0d;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0d0d0d;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);border:1px solid #f8c439;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(248,196,57,0.1);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <img src=""https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=logo%2Flogo.png&version_id=null"" alt=""MovieTheater Logo"" style=""max-width:180px;height:auto;filter:brightness(0) invert(1);"">
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#f8c439;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Verify Your Email</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #f8c439, #ffd700);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:16px;margin:0 0 24px 0;text-align:center;"">Thank you for registering with our cinema booking service. Please use the following code to verify your email address:</p>
      </div>

      <div style=""text-align:center;margin:32px 0;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #2a2a2a 0%, #3d3d3d 100%);border:2px solid #f8c439;border-radius:12px;padding:24px 32px;box-shadow:0 8px 24px rgba(248,196,57,0.2);"">
          <div style=""color:#f8c439;font-size:36px;font-weight:bold;letter-spacing:8px;font-family:monospace;"">{request.Otp}</div>
        </div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#b3b3b3;font-size:14px;margin:0;text-align:center;"">This code will expire in 10 minutes. If you didn't request this code, please ignore this email.</p>
      </div>

      <div style=""border-top:1px solid #333333;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Best regards,<br/><span style=""color:#f8c439;font-weight:600;"">MovieTheater Team</span></p>
      </div>

    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "OTP authentication at MovieTheater", html);
        }

        public async Task SendForgotPasswordOtpEmailAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#0d0d0d;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0d0d0d;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);border:1px solid #f8c439;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(248,196,57,0.1);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <img src=""https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=logo%2Flogo.png&version_id=null"" alt=""MovieTheater Logo"" style=""max-width:180px;height:auto;filter:brightness(0) invert(1);"">
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#f8c439;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Reset Your Password</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #f8c439, #ffd700);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:16px;margin:0 0 24px 0;text-align:center;"">We received a request to reset your password for your MovieTheater account. Please use the following code to proceed with password reset:</p>
      </div>

      <div style=""text-align:center;margin:32px 0;"">
        <div style=""display:inline-block;background:linear-gradient(135deg, #2a2a2a 0%, #3d3d3d 100%);border:2px solid #f8c439;border-radius:12px;padding:24px 32px;box-shadow:0 8px 24px rgba(248,196,57,0.2);"">
          <div style=""color:#f8c439;font-size:36px;font-weight:bold;letter-spacing:8px;font-family:monospace;"">{request.Otp}</div>
        </div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#b3b3b3;font-size:14px;margin:0;text-align:center;"">This code will expire in 15 minutes. If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
      </div>

      <div style=""border-top:1px solid #333333;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Best regards,<br/><span style=""color:#f8c439;font-weight:600;"">MovieTheater Team</span></p>
      </div>

    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "OTP password recovery at MovieTheater", html);
        }

        public async Task SendPasswordChangeSuccessAsync(EmailRequestDto request)
        {
            var html = $@"
<html style=""background-color:#0d0d0d;margin:0;padding:0;"">
  <body style=""font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;color:#ffffff;padding:40px 20px;background-color:#0d0d0d;line-height:1.6;"">
    <div style=""max-width:600px;margin:auto;background:linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);border:1px solid #f8c439;border-radius:16px;padding:40px;box-shadow:0 20px 40px rgba(248,196,57,0.1);"">

      <div style=""text-align:center;margin-bottom:32px;"">
        <img src=""https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=logo%2Flogo.png&version_id=null"" alt=""MovieTheater Logo"" style=""max-width:180px;height:auto;filter:brightness(0) invert(1);"">
      </div>

      <div style=""text-align:center;margin-bottom:32px;"">
        <h1 style=""color:#f8c439;font-size:32px;font-weight:bold;margin:0 0 8px 0;letter-spacing:-0.5px;"">Password Reset Successful!</h1>
        <div style=""width:60px;height:3px;background:linear-gradient(90deg, #f8c439, #ffd700);margin:16px auto;border-radius:2px;""></div>
      </div>

      <div style=""margin-bottom:32px;"">
        <p style=""color:#e5e5e5;font-size:18px;margin:0 0 16px 0;text-align:center;"">Hello {request.UserName},</p>
        <p style=""color:#e5e5e5;font-size:16px;margin:0 0 16px 0;text-align:center;"">Your password has been successfully reset for your MovieTheater account.</p>
        <p style=""color:#b3b3b3;font-size:16px;margin:0;text-align:center;"">You can now log in with your new password and continue booking tickets for your favorite movies.</p>
      </div>

      <div style=""text-align:center;margin:40px 0;"">
        <a href=""https://movietheater.ae-tao-fullstack-api.com/login"" style=""display:inline-block;background:linear-gradient(135deg, #f8c439 0%, #ffd700 100%);color:#000000;padding:16px 32px;text-decoration:none;border-radius:12px;font-weight:bold;font-size:16px;box-shadow:0 8px 24px rgba(248,196,57,0.3);transition:all 0.3s ease;"">Login Now</a>
      </div>

      <div style=""background:rgba(248,196,57,0.1);border:1px solid rgba(248,196,57,0.3);border-radius:8px;padding:16px;margin:32px 0;"">
        <p style=""color:#e5e5e5;font-size:14px;margin:0;text-align:center;"">If you didn't make this change or if you have any concerns about your account security, please contact our support team immediately.</p>
      </div>

      <div style=""border-top:1px solid #333333;padding-top:24px;margin-top:40px;"">
        <p style=""color:#888888;font-size:14px;margin:0;text-align:center;"">Best regards,<br/><span style=""color:#f8c439;font-weight:600;"">MovieTheater Team</span></p>
      </div>

    </div>
  </body>
</html>";
            await SendEmailAsync(request.To, "Password has been changed at MovieTheater", html);
        }

        /// <summary>
        ///     Sends an email to the employee with their login credentials.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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