using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EVServiceCenter.Application.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly string _supportPhone;
        private readonly string _baseUrl;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _supportPhone = _config["Support:Phone"] ?? "1900-EVSERVICE";
            _baseUrl = _config["App:BaseUrl"] ?? "https://localhost:5001";
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(to))
                throw new ArgumentNullException(nameof(to), "Email recipient cannot be null or empty");
            if (string.IsNullOrEmpty(subject))
                throw new ArgumentNullException(nameof(subject), "Email subject cannot be null or empty");
            if (string.IsNullOrEmpty(body))
                throw new ArgumentNullException(nameof(body), "Email body cannot be null or empty");

            // Debug: Log all email configurations
            Console.WriteLine($"DEBUG - Email Config:");
            Console.WriteLine($"Host: '{_config["Email:Host"]}'");
            Console.WriteLine($"Port: '{_config["Email:Port"]}'");
            Console.WriteLine($"User: '{_config["Email:User"]}'");
            Console.WriteLine($"From: '{_config["Email:From"]}'");
            Console.WriteLine($"FromName: '{_config["Email:FromName"]}'");

            // Get email configuration with validation
            var host = _config["Email:Host"];
            if (string.IsNullOrEmpty(host))
                throw new InvalidOperationException($"Email:Host configuration is missing. Value: '{host}'");

            if (!int.TryParse(_config["Email:Port"], out int port))
                throw new InvalidOperationException("Email:Port configuration is invalid or missing");

            var user = _config["Email:User"];
            if (string.IsNullOrEmpty(user))
                throw new InvalidOperationException("Email:User configuration is missing");

            var password = _config["Email:Password"];
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Email:Password configuration is missing");

            var from = _config["Email:From"];
            if (string.IsNullOrEmpty(from))
                throw new InvalidOperationException("Email:From configuration is missing");

            var fromName = _config["Email:FromName"];
            if (string.IsNullOrEmpty(fromName))
                throw new InvalidOperationException("Email:FromName configuration is missing");

            try
            {
                using var smtp = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(from, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(to);

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentName, byte[] attachmentContent, string contentType = "application/pdf")
        {
            if (string.IsNullOrEmpty(to)) throw new ArgumentNullException(nameof(to));
            if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(body)) throw new ArgumentNullException(nameof(body));
            if (attachmentContent == null || attachmentContent.Length == 0) throw new ArgumentNullException(nameof(attachmentContent));

            var host = _config["Email:Host"] ?? throw new InvalidOperationException("Email:Host configuration is missing");
            if (!int.TryParse(_config["Email:Port"], out int port)) throw new InvalidOperationException("Email:Port configuration is invalid or missing");
            var user = _config["Email:User"] ?? throw new InvalidOperationException("Email:User configuration is missing");
            var password = _config["Email:Password"] ?? throw new InvalidOperationException("Email:Password configuration is missing");
            var from = _config["Email:From"] ?? throw new InvalidOperationException("Email:From configuration is missing");
            var fromName = _config["Email:FromName"] ?? throw new InvalidOperationException("Email:FromName configuration is missing");

            try
            {
                using var smtp = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(from, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(to);
                mail.Attachments.Add(new Attachment(new System.IO.MemoryStream(attachmentContent), attachmentName, contentType));

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email with attachment: {ex.Message}", ex);
            }
        }

        public async Task SendVerificationEmailAsync(string toEmail, string fullName, string otpCode)
        {
            try
            {
                // Log OTP code to console for debugging
                Console.WriteLine($"🔐 OTP CODE FOR {toEmail}: {otpCode}");
                Console.WriteLine($"📧 Sending verification email to: {toEmail}");
                
                var subject = "Xác thực tài khoản EV Service Center";
                var body = CreateVerificationEmailTemplate(fullName, otpCode);
                
                await SendEmailAsync(toEmail, subject, body);
                
                Console.WriteLine($"✅ Verification email sent successfully to: {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send verification email to {toEmail}: {ex.Message}");
                throw new Exception($"Không thể gửi email xác thực: {ex.Message}");
            }
        }

            public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
            {
                try
                {
                    var subject = "Chào mừng bạn đến với EV Service Center!";
                    var body = CreateWelcomeEmailTemplate(fullName);
                    
                    await SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể gửi email chào mừng: {ex.Message}");
                }
            }

            public async Task SendResetPasswordEmailAsync(string toEmail, string fullName, string otpCode)
            {
                try
                {
                    var subject = "Yêu cầu đặt lại mật khẩu - EV Service Center";
                    var body = CreateResetPasswordEmailTemplate(fullName, otpCode);
                    
                    await SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể gửi email đặt lại mật khẩu: {ex.Message}");
                }
            }

        private string CreateVerificationEmailTemplate(string fullName, string otpCode)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.15); }}
        .header {{ background: linear-gradient(135deg, #465FFF, #6c5ce7); color: white; padding: 30px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.9; font-size: 16px; }}
        .content {{ padding: 40px 30px; }}
        .content h2 {{ color: #465FFF; margin-bottom: 20px; font-size: 24px; }}
        .otp-section {{ background: linear-gradient(135deg, #f8f9fa, #e3f2fd); padding: 30px; border-radius: 15px; text-align: center; margin: 25px 0; border-left: 4px solid #465FFF; }}
        .otp-code {{ font-size: 36px; font-weight: bold; color: #465FFF; letter-spacing: 8px; background: white; padding: 20px; border-radius: 10px; display: inline-block; margin: 15px 0; border: 2px dashed #465FFF; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 8px; padding: 15px; margin: 20px 0; }}
        .warning-icon {{ color: #f39c12; font-size: 18px; }}
        .steps {{ background-color: #f8f9fa; padding: 25px; border-radius: 10px; margin: 25px 0; }}
        .steps ol {{ padding-left: 20px; }}
        .steps li {{ padding: 8px 0; font-size: 16px; }}
        .footer {{ background-color: #f8f9fa; padding: 25px; text-align: center; font-size: 14px; color: #666; }}
        .divider {{ height: 3px; background: linear-gradient(90deg, #465FFF, #6c5ce7); margin: 25px 0; border-radius: 2px; }}
        .support-box {{ background-color: #e8f5e8; border: 1px solid #c3e6c3; border-radius: 8px; padding: 15px; margin-top: 20px; }}
        @media only screen and (max-width: 600px) {{
            .container {{ margin: 0 10px; }}
            .content {{ padding: 20px 15px; }}
            .otp-code {{ font-size: 28px; letter-spacing: 4px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>EV Service Center</h1>
            <p>Xác thực tài khoản của bạn</p>
        </div>
        <div class='content'>
            <h2>Xin chào {fullName}!</h2>
            <p style='font-size: 16px; line-height: 1.8;'>
                Cảm ơn bạn đã đăng ký tài khoản tại <strong style='color: #465FFF;'>EV Service Center</strong>. 
                Để hoàn tất quá trình đăng ký và kích hoạt tài khoản, vui lòng sử dụng mã xác thực bên dưới:
            </p>
            
            <div class='otp-section'>
                <h3 style='color: #465FFF; margin-bottom: 15px; font-size: 18px;'>MÃ XÁC THỰC CỦA BẠN</h3>
                <div class='otp-code'>{otpCode}</div>
                <p style='color: #666; font-size: 14px; margin-top: 15px;'>
                    Vui lòng nhập mã này vào trang xác thực tài khoản
                </p>
            </div>

            <div class='warning'>
                <p style='margin: 0;'><span class='warning-icon'>⚠️</span> <strong>Lưu ý quan trọng:</strong></p>
                <ul style='margin: 10px 0 0 20px; padding-left: 0;'>
                    <li>Mã xác thực này có hiệu lực trong <strong>15 phút</strong></li>
                    <li>Chỉ được sử dụng <strong>một lần duy nhất</strong></li>
                    <li>Không chia sẻ mã này với bất kỳ ai</li>
                    <li>Nếu hết hạn, bạn có thể yêu cầu gửi mã mới</li>
                </ul>
            </div>

            <div class='steps'>
                <h3 style='color: #465FFF; margin-bottom: 15px;'>Các bước tiếp theo:</h3>
                <ol>
                    <li>Trở lại trang đăng ký của EV Service Center</li>
                    <li>Nhập chính xác mã <strong>{otpCode}</strong> vào ô xác thực</li>
                    <li>Nhấn nút ""Xác thực tài khoản""</li>
                    <li>Tài khoản của bạn sẽ được kích hoạt ngay lập tức</li>
                </ol>
            </div>

            <div class='support-box'>
                <p style='margin: 0; color: #2d5016;'><strong>Cần hỗ trợ?</strong></p>
                <p style='margin: 5px 0 0 0; color: #2d5016;'>
                    Nếu bạn gặp khó khăn trong quá trình xác thực, vui lòng liên hệ:
                    <br>Email: support@evservicecenter.com
                    <br>Hotline: {_supportPhone}
                </p>
            </div>
        </div>
        <div class='footer'>
            <div class='divider'></div>
            <p><strong>© 2024 EV Service Center</strong> - Nền tảng dịch vụ xe điện hàng đầu</p>
            <p style='margin: 5px 0;'>
                Email: support@evservicecenter.com | Hotline: {_supportPhone}
            </p>
            <p style='font-size: 12px; color: #999; margin-top: 15px;'>
                Email này được gửi tự động, vui lòng không reply trực tiếp. <br>
                Nếu bạn không yêu cầu tạo tài khoản, vui lòng bỏ qua email này.
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string CreateWelcomeEmailTemplate(string fullName)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.15); }}
        .header {{ background: linear-gradient(135deg, #465FFF, #6c5ce7); color: white; padding: 30px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
        .content {{ padding: 40px 30px; }}
        .content h2 {{ color: #465FFF; margin-bottom: 20px; }}
        .features {{ background-color: #f8f9fa; padding: 25px; border-radius: 8px; margin: 25px 0; }}
        .features ul {{ list-style: none; padding: 0; }}
        .features li {{ padding: 8px 0; font-size: 16px; }}
        .features li:before {{ content: '✅ '; color: #28a745; font-weight: bold; }}
        .cta {{ text-align: center; margin: 30px 0; }}
        .btn {{ display: inline-block; background: linear-gradient(135deg, #465FFF, #6c5ce7); color: white; padding: 15px 30px; text-decoration: none; border-radius: 25px; font-weight: bold; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 14px; color: #666; }}
        .divider {{ height: 2px; background: linear-gradient(90deg, #465FFF, #6c5ce7); margin: 25px 0; border-radius: 1px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>EV Service Center</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Nền tảng dịch vụ xe điện hàng đầu</p>
        </div>
        <div class='content'>
            <h2>Chào mừng {fullName}!</h2>
            <p style='font-size: 16px; line-height: 1.8;'>
                Tài khoản của bạn đã được <strong style='color: #28a745;'>xác thực thành công</strong>! 
                Chào mừng bạn đến với gia đình <strong style='color: #465FFF;'>EV Service Center</strong>. 
                Bây giờ bạn có thể tận hưởng tất cả dịch vụ của chúng tôi.
            </p>
            
            <div class='divider'></div>
            
            <div class='features'>
                <h3 style='color: #465FFF; margin-bottom: 15px;'>Bạn có thể bắt đầu:</h3>
                <ul>
                    <li>Đặt lịch sửa chữa, bảo dưỡng xe điện</li>
                    <li>Theo dõi trạng thái công việc real-time</li>
                    <li>Xem lịch sử dịch vụ và hóa đơn</li>
                    <li>Nhận thông báo và ưu đãi đặc biệt</li>
                    <li>Tư vấn từ đội ngũ kỹ thuật viên chuyên nghiệp</li>
                </ul>
            </div>

            <div class='cta'>
                <p style='font-size: 16px; margin-bottom: 20px;'>
                    <strong>Sẵn sàng trải nghiệm dịch vụ tốt nhất?</strong>
                </p>
                <a href='{_baseUrl}/login' class='btn' style='color: white;'>Đăng nhập ngay</a>
            </div>
        </div>
        <div class='footer'>
            <div class='divider'></div>
            <p><strong>© 2024 EV Service Center</strong> - Tất cả quyền được bảo lưu</p>
            <p style='margin: 5px 0;'>
                Email: support@evservicecenter.com | Hotline: {_supportPhone}
            </p>
            <p style='font-size: 12px; color: #999; margin-top: 15px;'>
                Email này được gửi tự động, vui lòng không reply trực tiếp.
            </p>
        </div>
    </div>
</body>
                    </html>";
            }

            private string CreateResetPasswordEmailTemplate(string fullName, string otpCode)
            {
                return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 20px; }}
                            .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.15); }}
                            .header {{ background: linear-gradient(135deg, #dc3545, #c82333); color: white; padding: 30px 20px; text-align: center; }}
                            .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
                            .header p {{ margin: 10px 0 0 0; opacity: 0.9; font-size: 16px; }}
                            .content {{ padding: 40px 30px; }}
                            .content h2 {{ color: #dc3545; margin-bottom: 20px; font-size: 24px; }}
                            .otp-section {{ background: #f8d7da; padding: 30px; border-radius: 15px; text-align: center; margin: 25px 0; border-left: 4px solid #dc3545; }}
                            .otp-code {{ font-size: 36px; font-weight: bold; color: #dc3545; letter-spacing: 8px; background: white; padding: 20px; border-radius: 10px; display: inline-block; margin: 15px 0; border: 2px dashed #dc3545; }}
                            .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 8px; padding: 15px; margin: 20px 0; }}
                            .steps {{ background-color: #f8f9fa; padding: 25px; border-radius: 10px; margin: 25px 0; }}
                            .steps ol {{ padding-left: 20px; }}
                            .steps li {{ padding: 8px 0; font-size: 16px; }}
                            .footer {{ background-color: #f8f9fa; padding: 25px; text-align: center; font-size: 14px; color: #666; }}
                            .divider {{ height: 3px; background: linear-gradient(90deg, #dc3545, #c82333); margin: 25px 0; border-radius: 2px; }}
                            .support-box {{ background-color: #e8f5e8; border: 1px solid #c3e6c3; border-radius: 8px; padding: 15px; margin-top: 20px; }}
                            @media only screen and (max-width: 600px) {{
                                .container {{ margin: 0 10px; }}
                                .content {{ padding: 20px 15px; }}
                                .otp-code {{ font-size: 28px; letter-spacing: 4px; }}
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>EV Service Center</h1>
                                <p>Yêu cầu đặt lại mật khẩu</p>
                            </div>
                            <div class='content'>
                                <h2>Xin chào {fullName}!</h2>
                                <p style='font-size: 16px; line-height: 1.8;'>
                                    Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. 
                                    Để tiếp tục, vui lòng sử dụng mã xác thực bên dưới:
                                </p>
                                
                                <div class='otp-section'>
                                    <h3 style='color: #dc3545; margin-bottom: 15px; font-size: 18px;'>MÃ XÁC THỰC ĐẶT LẠI MẬT KHẨU</h3>
                                    <div class='otp-code'>{otpCode}</div>
                                    <p style='color: #666; font-size: 14px; margin-top: 15px;'>
                                        Vui lòng nhập mã này vào trang đặt lại mật khẩu
                                    </p>
                                </div>

                                <div class='warning'>
                                    <p style='margin: 0;'><strong>Lưu ý quan trọng:</strong></p>
                                    <ul style='margin: 10px 0 0 20px; padding-left: 0;'>
                                        <li>Mã xác thực này có hiệu lực trong <strong>15 phút</strong></li>
                                        <li>Chỉ được sử dụng <strong>một lần duy nhất</strong></li>
                                        <li>Không chia sẻ mã này với bất kỳ ai</li>
                                        <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                                    </ul>
                                </div>

                                <div class='steps'>
                                    <h3 style='color: #dc3545; margin-bottom: 15px;'>Các bước tiếp theo:</h3>
                                    <ol>
                                        <li>Trở lại trang đặt lại mật khẩu của EV Service Center</li>
                                        <li>Nhập chính xác mã <strong>{otpCode}</strong> vào ô xác thực</li>
                                        <li>Nhập mật khẩu mới và xác nhận</li>
                                        <li>Nhấn nút ""Đặt lại mật khẩu""</li>
                                    </ol>
                                </div>

                                <div class='support-box'>
                                    <p style='margin: 0; color: #2d5016;'><strong>Cần hỗ trợ?</strong></p>
                                    <p style='margin: 5px 0 0 0; color: #2d5016;'>
                                        Nếu bạn gặp khó khăn trong quá trình đặt lại mật khẩu, vui lòng liên hệ:
                                        <br>Email: support@evservicecenter.com
                                        <br>Hotline: {_supportPhone}
                                    </p>
                                </div>
                            </div>
                            <div class='footer'>
                                <div class='divider'></div>
                                <p><strong>2024 EV Service Center</strong> - Nền tảng dịch vụ xe điện hàng đầu</p>
                                <p style='margin: 5px 0;'>
                                    Email: support@evservicecenter.com | Hotline: {_supportPhone}
                                </p>
                                <p style='font-size: 12px; color: #999; margin-top: 15px;'>
                                    Email này được gửi tự động, vui lòng không reply trực tiếp. <br>
                                    Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";
            }
        }
    }