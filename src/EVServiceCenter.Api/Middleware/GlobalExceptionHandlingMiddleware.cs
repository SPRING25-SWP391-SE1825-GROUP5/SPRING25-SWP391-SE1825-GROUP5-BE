using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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
