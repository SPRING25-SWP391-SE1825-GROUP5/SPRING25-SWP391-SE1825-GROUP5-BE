using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Api.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new
            {
                success = false,
                message = "Có lỗi xảy ra trong quá trình xử lý yêu cầu",
                error = GetErrorCode(exception),
                timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        success = false,
                        message = exception.Message,
                        error = "INVALID_REQUEST",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse = new
                    {
                        success = false,
                        message = "Bạn không có quyền truy cập tài nguyên này",
                        error = "UNAUTHORIZED",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = new
                    {
                        success = false,
                        message = "Không tìm thấy tài nguyên được yêu cầu",
                        error = "NOT_FOUND",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        success = false,
                        message = exception.Message,
                        error = "INVALID_OPERATION",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case NotImplementedException:
                    response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    errorResponse = new
                    {
                        success = false,
                        message = "Tính năng chưa được triển khai",
                        error = "NOT_IMPLEMENTED",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case DbUpdateException dbEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var dbErrorMessage = "Lỗi cơ sở dữ liệu";
                    
                    // Xử lý Foreign Key constraint violation
                    if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547)
                    {
                        dbErrorMessage = "Dữ liệu không hợp lệ: Tham chiếu đến bản ghi không tồn tại. ";
                        // Extract table name from error message
                        var match = Regex.Match(sqlEx.Message, @"table ['""](\w+)['""]");
                        if (match.Success)
                        {
                            var tableName = match.Groups[1].Value;
                            if (tableName == "Customers")
                            {
                                dbErrorMessage = $"Không tìm thấy khách hàng với ID được chỉ định. Vui lòng kiểm tra lại.";
                            }
                            else if (tableName == "Promotions")
                            {
                                dbErrorMessage = "Không tìm thấy mã khuyến mãi. Vui lòng kiểm tra lại.";
                            }
                            else
                            {
                                dbErrorMessage += $"Bảng {tableName} không tồn tại hoặc dữ liệu không hợp lệ.";
                            }
                        }
                    }
                    
                    errorResponse = new
                    {
                        success = false,
                        message = dbErrorMessage,
                        error = "DATABASE_ERROR",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = new
                    {
                        success = false,
                        message = "Có lỗi xảy ra trong quá trình xử lý yêu cầu",
                        error = "INTERNAL_SERVER_ERROR",
                        timestamp = DateTime.UtcNow
                    };
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(jsonResponse);
        }

        private static string GetErrorCode(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "ARGUMENT_NULL",
                ArgumentException => "ARGUMENT_INVALID",
                UnauthorizedAccessException => "UNAUTHORIZED",
                KeyNotFoundException => "NOT_FOUND",
                InvalidOperationException => "INVALID_OPERATION",
                NotImplementedException => "NOT_IMPLEMENTED",
                _ => "UNKNOWN_ERROR"
            };
        }
    }
}
