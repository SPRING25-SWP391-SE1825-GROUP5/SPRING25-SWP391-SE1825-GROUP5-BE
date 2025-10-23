using System;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayOSService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _baseUrl;

        public PayOSService(IConfiguration configuration, ILogger<PayOSService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            
            _clientId = _configuration["PayOS:ClientId"] ?? throw new ArgumentNullException("PayOS:ClientId");
            _apiKey = _configuration["PayOS:ApiKey"] ?? throw new ArgumentNullException("PayOS:ApiKey");
            _checksumKey = _configuration["PayOS:ChecksumKey"] ?? throw new ArgumentNullException("PayOS:ChecksumKey");
            _baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn/v2";
        }

        private string ComputeHmacSha256Hex(string data, string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
            return sb.ToString();
        }

        public async Task<string> CreatePaymentLinkAsync(int bookingId, decimal amount, string description, string? customerName = null)
        {
            try
            {
                _logger.LogInformation($"Tạo link thanh toán cho booking {bookingId} với số tiền {amount}");

                // Tạo orderCode từ bookingId (giống code cũ)
                var orderCode = bookingId; // PayOS yêu cầu là số

                // Lấy URL callback từ config
                var returnUrl = _configuration["PayOS:ReturnUrl"];
                var cancelUrl = _configuration["PayOS:CancelUrl"];

                _logger.LogInformation($"ReturnURL: {returnUrl}, CancelURL: {cancelUrl}");

                // Tính amount theo VNĐ integer (giống code cũ)
                var amountVnd = (int)Math.Round(amount);
                if (amountVnd < 1000) amountVnd = 1000; // Min amount

                // Tạo canonical string theo thứ tự cố định (giống code cũ)
                var canonical = $"amount={amountVnd}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                var signature = ComputeHmacSha256Hex(canonical, _checksumKey);

                // Tạo dữ liệu thanh toán theo format code cũ
                var paymentData = new
                {
                    orderCode = orderCode,
                    amount = amountVnd, // VNĐ integer
                    description = description,
                    items = new[]
                    {
                        new
                        {
                            name = $"Booking #{bookingId}",
                            quantity = 1,
                            price = amountVnd
                        }
                    },
                    returnUrl = returnUrl,
                    cancelUrl = cancelUrl,
                    signature = signature
                };

                _logger.LogInformation($"PayOS Payment Data: {JsonSerializer.Serialize(paymentData)}");
                _logger.LogInformation($"PayOS Canonical: {canonical}");
                _logger.LogInformation($"PayOS Signature: {signature}");
                _logger.LogInformation($"PayOS Headers - ClientId: {_clientId}, ApiKey: {_apiKey.Substring(0, 8)}...");

                // Gọi API PayOS
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/payment-requests");
                request.Headers.Add("x-client-id", _clientId);
                request.Headers.Add("x-api-key", _apiKey);
                
                var json = JsonSerializer.Serialize(paymentData);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"PayOS API error: {response.StatusCode} - {responseContent}");
                }

                // Parse PayOS response
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                
                _logger.LogInformation($"PayOS Response: {responseContent}");
                
                // Check if success
                if (root.TryGetProperty("code", out var codeElement) && codeElement.GetString() == "00")
                {
                    if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
                    {
                        // Get qrCode for banking apps (VietQR)
                        if (dataElement.TryGetProperty("qrCode", out var qrCodeElement))
                        {
                            var qrCode = qrCodeElement.GetString();
                            _logger.LogInformation($"PayOS VietQR Code: {qrCode}");
                            
                            if (!string.IsNullOrEmpty(qrCode))
                            {
                                _logger.LogInformation($"Đã tạo thành công VietQR cho booking {bookingId}");
                                return qrCode; // Return VietQR string for banking apps
                            }
                        }
                        
                        // Fallback to checkoutUrl (for PayOS app)
                        if (dataElement.TryGetProperty("checkoutUrl", out var checkoutUrlElement))
                        {
                            var checkoutUrl = checkoutUrlElement.GetString();
                            _logger.LogInformation($"PayOS Checkout URL: {checkoutUrl}");
                            return checkoutUrl ?? string.Empty;
                        }
                    }
                }
                
                // Error case
                var errorDesc = root.TryGetProperty("desc", out var descElement) ? descElement.GetString() : "Unknown error";
                _logger.LogError($"PayOS Error: {errorDesc}. Full response: {responseContent}");
                throw new Exception($"PayOS lỗi: {errorDesc}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tạo payment link cho booking {bookingId}");
                throw new Exception($"Không thể tạo link thanh toán: {ex.Message}");
            }
        }

        public async Task<PayOSPaymentInfo?> GetPaymentInfoAsync(int orderCode)
        {
            try
            {
                _logger.LogInformation($"Lấy thông tin thanh toán cho orderCode: {orderCode}");

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/payment-requests/{orderCode}");
                request.Headers.Add("x-client-id", _clientId);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS Payment Info Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"PayOS API error: {response.StatusCode} - {responseContent}");
                }

                var payosResponse = JsonSerializer.Deserialize<PayOSResponse>(responseContent);
                return payosResponse?.Data;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy payment info cho orderCode {orderCode}");
                throw new Exception($"Không thể lấy thông tin thanh toán: {ex.Message}");
            }
        }

        public async Task<bool> CancelPaymentLinkAsync(int orderCode)
        {
            try
            {
                _logger.LogInformation($"Hủy payment link với orderCode: {orderCode}");

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/payment-requests/{orderCode}");
                request.Headers.Add("x-client-id", _clientId);
                request.Headers.Add("x-api-key", _apiKey);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS Cancel Response: {responseContent}");

                return response.IsSuccessStatusCode;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy payment link cho orderCode {orderCode}");
                return false;
            }
        }

        public bool VerifyPaymentWebhook(string webhookData)
        {
            try
            {
                // Implement webhook verification logic
                // PayOS sẽ gửi signature để verify
                _logger.LogInformation("Verifying PayOS webhook");
                return true; // Simplified for now
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi verify webhook");
                return false;
            }
        }

        /// <summary>
        /// Xử lý callback từ PayOS sau khi thanh toán thành công
        /// </summary>
        public async Task<bool> HandlePaymentCallbackAsync(string orderCode)
        {
            try
            {
                _logger.LogInformation($"Xử lý callback thanh toán cho orderCode: {orderCode}");
                
                // Lấy thông tin thanh toán từ PayOS
                var paymentInfo = await GetPaymentInfoAsync(int.Parse(orderCode));
                if (paymentInfo == null)
                {
                    _logger.LogWarning($"Không tìm thấy thông tin thanh toán cho orderCode: {orderCode}");
                    return false;
                }

                // Kiểm tra trạng thái thanh toán
                if (paymentInfo.Status != "PAID")
                {
                    _logger.LogInformation($"Thanh toán chưa thành công cho orderCode: {orderCode}, status: {paymentInfo.Status}");
                    return false;
                }

                _logger.LogInformation($"Thanh toán thành công cho orderCode: {orderCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xử lý callback thanh toán cho orderCode: {orderCode}");
                return false;
            }
        }
    }

    // DTOs cho PayOS API
    public class PayOSResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public PayOSPaymentInfo? Data { get; set; }
    }

}
