using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.Controllers;

/// <summary>
/// Base controller with common functionality
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseController(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles exceptions and returns appropriate API response
    /// </summary>
    protected IActionResult HandleException(Exception ex, string operation = "Operation")
    {
        _logger.LogError(ex, "Error occurred during {Operation}", operation);

        // Handle specific exception types
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

    /// <summary>
    /// Validates model state and returns bad request if invalid
    /// </summary>
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

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    protected int? GetCurrentUserId()
    {
        // Try different claim types for user ID
        var userIdClaim = User.FindFirst("nameid") 
                         ?? User.FindFirst("UserId")
                         ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user role from claims
    /// </summary>
    protected string? GetCurrentUserRole()
    {
        return User.FindFirst("Role")?.Value;
    }

    /// <summary>
    /// Checks if the current user has the specified role
    /// </summary>
    protected bool HasRole(string role)
    {
        return GetCurrentUserRole()?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Checks if the current user is admin
    /// </summary>
    protected bool IsAdmin()
    {
        return HasRole("ADMIN");
    }

    /// <summary>
    /// Checks if the current user is technician
    /// </summary>
    protected bool IsTechnician()
    {
        return HasRole("TECHNICIAN");
    }

    /// <summary>
    /// Checks if the current user is customer
    /// </summary>
    protected bool IsCustomer()
    {
        return HasRole("CUSTOMER");
    }
}
