using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected BaseController(ILogger logger)
    {
        _ = logger;
    }

    protected IActionResult HandleException(Exception ex, string operation = "Operation")
    {
        return ex switch
        {
            ArgumentNullException => BadRequest(new { success = false, message = ex.Message }),
            ArgumentException => BadRequest(new { success = false, message = ex.Message }),
            InvalidOperationException => BadRequest(new { success = false, message = ex.Message }),
            UnauthorizedAccessException => Unauthorized(new { success = false, message = ex.Message }),
            NotImplementedException => StatusCode(500, new { success = false, message = "Tính năng chưa được triển khai" }),
            Microsoft.EntityFrameworkCore.DbUpdateException dbEx => StatusCode(500, new { 
                success = false, 
                message = "Lỗi cơ sở dữ liệu", 
                details = dbEx.InnerException?.Message ?? dbEx.Message,
                operation = operation
            }),
            Microsoft.Data.SqlClient.SqlException sqlEx => StatusCode(500, new { 
                success = false, 
                message = "Lỗi SQL Server", 
                details = sqlEx.Message,
                errorNumber = sqlEx.Number,
                operation = operation
            }),
            _ => StatusCode(500, new { 
                success = false, 
                message = "Có lỗi xảy ra trong quá trình xử lý", 
                details = ex.Message,
                operation = operation
            })
        };
    }

    protected IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
        }

        return null;
    }

    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("nameid") 
                         ?? User.FindFirst("UserId")
                         ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    protected string? GetCurrentUserRole()
    {
        return User.FindFirst("Role")?.Value;
    }

    protected bool HasRole(string role)
    {
        return GetCurrentUserRole()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
    }

    protected bool IsAdmin()
    {
        return HasRole("ADMIN");
    }

    protected bool IsTechnician()
    {
        return HasRole("TECHNICIAN");
    }

    protected bool IsCustomer()
    {
        return HasRole("CUSTOMER");
    }
}
