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
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly string _supportPhone;
        private readonly string _baseUrl;

        public EmailService(IConfiguration config, IEmailTemplateRenderer templateRenderer)
        {
            _config = config;
            _templateRenderer = templateRenderer;
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
            // Email configuration loaded successfully

            // Get email configuration with validation
            var host = _config["Email:Host"];
            if (string.IsNullOrEmpty(host))
                throw new InvalidOperationException($"Email:Host configuration is missing. Value: '{host}'");

            if (!int.TryParse(_config["Email:Port"], out int port))
                throw new InvalidOperationException("Cấu hình Email:Port không hợp lệ hoặc thiếu");

            var user = _config["Email:User"];
            if (string.IsNullOrEmpty(user))
                throw new InvalidOperationException("Thiếu cấu hình Email:User");

            var password = _config["Email:Password"];
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Thiếu cấu hình Email:Password");

            var from = _config["Email:From"];
            if (string.IsNullOrEmpty(from))
                throw new InvalidOperationException("Thiếu cấu hình Email:From");

            var fromName = _config["Email:FromName"];
            if (string.IsNullOrEmpty(fromName))
                throw new InvalidOperationException("Thiếu cấu hình Email:FromName");

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
                throw new Exception($"Gửi email thất bại: {ex.Message}", ex);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentName, byte[] attachmentContent, string contentType = "application/pdf")
        {
            if (string.IsNullOrEmpty(to)) throw new ArgumentNullException(nameof(to));
            if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(body)) throw new ArgumentNullException(nameof(body));
            if (attachmentContent == null || attachmentContent.Length == 0) throw new ArgumentNullException(nameof(attachmentContent));

            var host = _config["Email:Host"] ?? throw new InvalidOperationException("Thiếu cấu hình Email:Host");
            if (!int.TryParse(_config["Email:Port"], out int port)) throw new InvalidOperationException("Cấu hình Email:Port không hợp lệ hoặc thiếu");
            var user = _config["Email:User"] ?? throw new InvalidOperationException("Thiếu cấu hình Email:User");
            var password = _config["Email:Password"] ?? throw new InvalidOperationException("Thiếu cấu hình Email:Password");
            var from = _config["Email:From"] ?? throw new InvalidOperationException("Thiếu cấu hình Email:From");
            var fromName = _config["Email:FromName"] ?? throw new InvalidOperationException("Thiếu cấu hình Email:FromName");

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
                throw new Exception($"Gửi email kèm tệp thất bại: {ex.Message}", ex);
            }
        }

        public async Task SendVerificationEmailAsync(string toEmail, string fullName, string otpCode)
        {
            try
            {
                // Log OTP code to console for debugging
                // OTP code generated and email being sent
                
                var subject = "Xác thực tài khoản EV Service Center";
                var body = await _templateRenderer.RenderAsync(
                    "otp-verification",
                    new System.Collections.Generic.Dictionary<string, string>
                    {
                        {"fullName", fullName},
                        {"otpCode", otpCode},
                        {"expireMinutes", (_config["OTP:ExpireMinutes"] ?? "15")},
                        {"baseUrl", _baseUrl},
                        {"supportPhone", _supportPhone},
                        {"year", DateTime.UtcNow.Year.ToString()},
                        {"logoUrl", _baseUrl.TrimEnd('/') + "/email/logo.webp"}
                    }
                );
                
                await SendEmailAsync(toEmail, subject, body);
                
                // Verification email sent successfully
            }
            catch (Exception ex)
            {
                // Failed to send verification email
                throw new Exception($"Không thể gửi email xác thực: {ex.Message}");
            }
        }

            public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
            {
                try
                {
                    var subject = "Chào mừng bạn đến với EV Service Center!";
                    var body = await _templateRenderer.RenderAsync(
                        "welcome",
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            {"fullName", fullName},
                            {"baseUrl", _baseUrl},
                            {"supportPhone", _supportPhone},
                            {"year", DateTime.UtcNow.Year.ToString()}
                        }
                    );
                    
                    await SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể gửi email chào mừng: {ex.Message}");
                }
            }

            // Removed inline reset-password template; now using external file via renderer

            public async Task SendResetPasswordEmailAsync(string toEmail, string fullName, string otpCode)
            {
                try
                {
                    var subject = "Yêu cầu đặt lại mật khẩu - EV Service Center";
                    var body = await _templateRenderer.RenderAsync(
                        "reset-password",
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            {"fullName", fullName},
                            {"otpCode", otpCode},
                            {"expireMinutes", (_config["OTP:ExpireMinutes"] ?? "15")},
                            {"baseUrl", _baseUrl},
                            {"supportPhone", _supportPhone},
                            {"year", DateTime.UtcNow.Year.ToString()}
                        }
                    );
                    await SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể gửi email đặt lại mật khẩu: {ex.Message}");
                }
            }

        
            // Removed inline reset-password template; now using external file via renderer

            public async Task SendWelcomeCustomerWithPasswordAsync(string toEmail, string fullName, string tempPassword)
            {
                try
                {
                    var subject = "Tài khoản khách hàng tại EV Service Center";
                    var body = await _templateRenderer.RenderAsync(
                        "welcome-customer",
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            {"fullName", fullName},
                            {"tempPassword", tempPassword},
                            {"baseUrl", _baseUrl},
                            {"supportPhone", _supportPhone},
                            {"year", DateTime.UtcNow.Year.ToString()}
                        }
                    );
                    await SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể gửi email chào mừng kèm mật khẩu: {ex.Message}");
                }
            }
        }
    }
