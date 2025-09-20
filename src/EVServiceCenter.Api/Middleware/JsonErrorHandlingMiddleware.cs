using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace EVServiceCenter.Api.Middleware
{
    public class JsonErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JsonErrorHandlingMiddleware> _logger;

        public JsonErrorHandlingMiddleware(RequestDelegate next, ILogger<JsonErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "JSON deserialization error occurred");
                
                var friendlyError = ConvertJsonErrorToFriendlyMessage(jsonEx.Message);
                
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = new[] { friendlyError }
                };
                
                var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                await context.Response.WriteAsync(jsonResponse);
            }
            catch (Exception ex) when (ex.Message.Contains("invalid end of a number") || 
                                       ex.Message.Contains("Expected a delimiter") ||
                                       ex.Message.Contains("Expected a number"))
            {
                _logger.LogWarning(ex, "Model binding error occurred");
                
                var friendlyError = ConvertJsonErrorToFriendlyMessage(ex.Message);
                
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = new[] { friendlyError }
                };
                
                var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                await context.Response.WriteAsync(jsonResponse);
            }
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
