using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _paymentUrl;
        private readonly string _returnUrl;
        private readonly string _ipnUrl;
        private readonly string _version;
        private readonly string _command;
        private readonly string _currCode;
        private readonly string _locale;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _tmnCode = _configuration["VNPay:TmnCode"] ?? throw new ArgumentNullException("VNPay:TmnCode");
            _hashSecret = _configuration["VNPay:HashSecret"] ?? throw new ArgumentNullException("VNPay:HashSecret");
            _paymentUrl = _configuration["VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _returnUrl = _configuration["VNPay:ReturnUrl"] ?? throw new ArgumentNullException("VNPay:ReturnUrl");
            _ipnUrl = _configuration["VNPay:IPNUrl"] ?? throw new ArgumentNullException("VNPay:IPNUrl");
            _version = _configuration["VNPay:Version"] ?? "2.1.0";
            _command = _configuration["VNPay:Command"] ?? "pay";
            _currCode = _configuration["VNPay:CurrCode"] ?? "VND";
            _locale = _configuration["VNPay:Locale"] ?? "vn";
        }

        public async Task<string> CreatePaymentUrlAsync(int bookingId, decimal amount, string description, string? customerName = null)
        {
            try
            {
                _logger.LogInformation("Creating VNPay payment URL for booking {BookingId}, amount: {Amount}", bookingId, amount);

                // Convert amount to VND (integer) - VNPay yêu cầu số tiền tính bằng xu (cents)
                // Ví dụ: 50000 VND = 5000000 xu
                var amountVnd = (long)Math.Round(amount * 100);

                // Tạo order ID (format: bookingId + timestamp để đảm bảo unique)
                // Format: "{bookingId}{timestamp}" - Ví dụ: "1571234567890"
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var orderId = $"{bookingId}{timestamp}";

                // Tạo payment data
                var vnpayData = new Dictionary<string, string>
                {
                    {"vnp_Version", _version},
                    {"vnp_Command", _command},
                    {"vnp_TmnCode", _tmnCode},
                    {"vnp_Amount", amountVnd.ToString()},
                    {"vnp_CurrCode", _currCode},
                    {"vnp_TxnRef", orderId},
                    {"vnp_OrderInfo", description},
                    {"vnp_OrderType", "other"},
                    {"vnp_Locale", _locale},
                    {"vnp_ReturnUrl", _returnUrl},
                    {"vnp_IpAddr", EVServiceCenter.Application.Constants.AppConstants.DefaultIpAddress.Localhost}, // Có thể lấy từ request sau
                    {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")}
                };

                // Thêm customer info nếu có
                if (!string.IsNullOrEmpty(customerName))
                {
                    vnpayData["vnp_Bill_FirstName"] = customerName;
                }

                // Sắp xếp và tạo query string
                var sortedData = vnpayData.OrderBy(x => x.Key).ToList();
                var queryString = string.Join("&", sortedData.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

                // Tạo hash
                var secureHash = CreateSecureHash(queryString);
                queryString += $"&vnp_SecureHash={secureHash}";

                // Tạo payment URL
                var paymentUrl = $"{_paymentUrl}?{queryString}";

                _logger.LogInformation("VNPay payment URL created successfully for booking {BookingId}", bookingId);
                return await Task.FromResult(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL for booking {BookingId}", bookingId);
                throw;
            }
        }

        public bool VerifyPaymentResponse(Dictionary<string, string> vnpayData, string vnp_SecureHash)
        {
            try
            {
                // Lấy hash từ response
                var receivedHash = vnp_SecureHash;

                // Loại bỏ vnp_SecureHash và vnp_SecureHashType khỏi data để verify
                var dataToVerify = vnpayData
                    .Where(x => x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                    .OrderBy(x => x.Key)
                    .ToList();

                var queryString = string.Join("&", dataToVerify.Select(x => $"{x.Key}={x.Value}"));
                var calculatedHash = CreateSecureHash(queryString);

                var isValid = calculatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);

                _logger.LogInformation("VNPay payment verification: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying VNPay payment response");
                return false;
            }
        }

        public int? GetBookingIdFromResponse(Dictionary<string, string> vnpayData)
        {
            try
            {
                // VNPay gửi vnp_TxnRef (orderId) có format: bookingId + timestamp
                // Ví dụ: "1571234567890" -> bookingId = 157, timestamp = 1234567890
                if (vnpayData.TryGetValue("vnp_TxnRef", out var txnRef))
                {
                    if (string.IsNullOrEmpty(txnRef))
                        return null;

                    // Tách bookingId từ txnRef
                    // Format: bookingId (1-6 chữ số) + timestamp (10 chữ số)
                    // Thử parse từ độ dài 1 đến 6 chữ số đầu
                    for (int len = 1; len <= 6 && len <= txnRef.Length; len++)
                    {
                        var bookingIdStr = txnRef.Substring(0, len);
                        if (int.TryParse(bookingIdStr, out var bookingId) && bookingId > 0 && bookingId < 1000000)
                        {
                            // Kiểm tra phần còn lại có phải timestamp không (10 chữ số)
                            var remaining = txnRef.Substring(len);
                            if (remaining.Length >= 10 && long.TryParse(remaining, out _))
                            {
                                _logger.LogInformation("Extracted bookingId {BookingId} from vnp_TxnRef: {TxnRef}", bookingId, txnRef);
                                return bookingId;
                            }
                        }
                    }

                    // Fallback: nếu không parse được, thử lấy bookingId trực tiếp (nếu txnRef chỉ là bookingId)
                    if (int.TryParse(txnRef, out var directBookingId) && directBookingId > 0 && directBookingId < 1000000)
                    {
                        _logger.LogInformation("Extracted bookingId {BookingId} directly from vnp_TxnRef: {TxnRef}", directBookingId, txnRef);
                        return directBookingId;
                    }
                }

                // Fallback: thử parse từ OrderInfo nếu có format "Pay{bookingId}ment"
                if (vnpayData.TryGetValue("vnp_OrderInfo", out var orderInfo))
                {
                    var payIndex = orderInfo.IndexOf("Pay", StringComparison.OrdinalIgnoreCase);
                    if (payIndex >= 0)
                    {
                        var mentIndex = orderInfo.IndexOf("ment", payIndex + 3, StringComparison.OrdinalIgnoreCase);
                        if (mentIndex > payIndex + 3)
                        {
                            var bookingIdStr = orderInfo.Substring(payIndex + 3, mentIndex - payIndex - 3);
                            if (int.TryParse(bookingIdStr, out var bookingId))
                            {
                                return bookingId;
                            }
                        }
                    }
                }

                _logger.LogWarning("Could not extract bookingId from VNPay response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting bookingId from VNPay response");
                return null;
            }
        }

        private string CreateSecureHash(string queryString)
        {
            // VNPay sử dụng HMACSHA512
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_hashSecret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            return string.Join("", hashBytes.Select(b => b.ToString("x2"))).ToUpper();
        }
    }
}

