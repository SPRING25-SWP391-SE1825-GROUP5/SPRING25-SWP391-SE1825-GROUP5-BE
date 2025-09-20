using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("/error")]
        public IActionResult Error()
        {
            var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            if (exception == null)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi hệ thống",
                    error = "UNKNOWN_ERROR",
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            // Xử lý lỗi JSON deserialization và model binding
            if (exception is JsonException jsonEx)
            {
                var friendlyError = ConvertJsonErrorToFriendlyMessage(jsonEx.Message);
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = new[] { friendlyError }
                });
            }

            // Xử lý lỗi model binding (thường là BadHttpRequestException)
            if (exception.Message.Contains("invalid end of a number") ||
                exception.Message.Contains("Expected a delimiter") ||
                exception.Message.Contains("Expected a number") ||
                exception.Message.Contains("The JSON value could not be converted"))
            {
                var friendlyError = ConvertJsonErrorToFriendlyMessage(exception.Message);
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = new[] { friendlyError }
                });
            }

            // Xử lý lỗi validation
            if (exception is ArgumentException argEx)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = new[] { argEx.Message }
                });
            }

            // Xử lý lỗi chung
            return StatusCode(500, new
            {
                success = false,
                message = "Đã xảy ra lỗi hệ thống",
                error = "INTERNAL_SERVER_ERROR",
                timestamp = DateTime.UtcNow
            });
        }

        private string ConvertJsonErrorToFriendlyMessage(string errorMessage)
        {
            // Pattern để extract field name từ JSON error
            var fieldMatch = Regex.Match(errorMessage, @"Path: \$\.(\w+)");
            if (fieldMatch.Success)
            {
                var fieldName = fieldMatch.Groups[1].Value;
                var friendlyFieldName = GetFriendlyFieldName(fieldName);
                
                // Kiểm tra các loại lỗi khác nhau
                if (errorMessage.Contains("invalid end of a number") || 
                    errorMessage.Contains("Expected a delimiter"))
                {
                    return $"{friendlyFieldName} phải là số hợp lệ";
                }
                
                if (errorMessage.Contains("Expected a number"))
                {
                    return $"{friendlyFieldName} phải là số";
                }
                
                if (errorMessage.Contains("Expected a boolean"))
                {
                    return $"{friendlyFieldName} phải là true hoặc false";
                }
                
                if (errorMessage.Contains("Expected a string"))
                {
                    return $"{friendlyFieldName} phải là chuỗi văn bản";
                }
                
                if (errorMessage.Contains("The JSON value could not be converted"))
                {
                    return $"{friendlyFieldName} có định dạng không hợp lệ";
                }
                
                return $"{friendlyFieldName} có định dạng không hợp lệ";
            }
            
            return "Dữ liệu JSON không hợp lệ";
        }

        private string GetFriendlyFieldName(string fieldName)
        {
            return fieldName.ToLower() switch
            {
                "customerid" => "ID khách hàng",
                "vehicleid" => "ID phương tiện",
                "centerid" => "ID trung tâm",
                "modelid" => "ID model xe",
                "modelbrand" => "Thương hiệu xe",
                "modelname" => "Tên model xe",
                "modelyear" => "Năm sản xuất",
                "batterycapacity" => "Dung lượng pin",
                "range" => "Tầm hoạt động",
                "currentmileage" => "Số km hiện tại",
                "unitprice" => "Đơn giá",
                "discountvalue" => "Giá trị giảm giá",
                "minorderamount" => "Số tiền đơn hàng tối thiểu",
                "maxdiscount" => "Giảm giá tối đa",
                "usagelimit" => "Giới hạn sử dụng",
                "userlimit" => "Giới hạn người dùng",
                "startslotid" => "ID slot bắt đầu",
                "endslotid" => "ID slot kết thúc",
                "lastservicedate" => "Ngày dịch vụ cuối",
                _ => fieldName
            };
        }
    }
}
