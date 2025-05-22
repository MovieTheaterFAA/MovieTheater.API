using Microsoft.Extensions.Configuration;
using MovieTheater.Domain.DTOs.EmailDTOs;
using Resend;

namespace MovieTheater.Application.Services
{
    public class EmailService
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
                        <h1 style=""color:#d02a2a;font-size:22px;"">Chào mừng {request.UserName}!</h1>
                        <p>Bạn đã đăng ký thành công tài khoản tại BlindTreasure.</p>
                        <p>Chúc bạn có trải nghiệm tuyệt vời.</p>
                        <p style=""margin-top:30px;"">Trân trọng,<br/>Đội ngũ BlindTreasure</p>
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
                      <div style=""max-width:600px;margin:auto;background:#fff;border:1px solid #d02a2a;border-radius:6px;padding:20px;text-align:center;"">
                        <h1 style=""color:#d02a2a;font-size:22px;"">Xác thực OTP</h1>
                        <p>Mã của bạn:</p>
                        <p style=""font-size:28px;color:#d02a2a;font-weight:bold;"">{request.Otp}</p>
                        <p style=""font-size:14px;"">Mã hết hạn sau 10 phút. Không chia sẻ với người khác.</p>
                        <p style=""margin-top:30px;"">Trân trọng,<br/>Đội ngũ BlindTreasure</p>
                      </div>
                    </body>
                  </html>";
            await SendEmailAsync(request.To, "Xác thực OTP tại BlindTreasure", html);
        }
    }
}
