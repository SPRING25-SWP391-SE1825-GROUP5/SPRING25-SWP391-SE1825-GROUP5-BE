using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            try
            {
                // Simple health check - just verify API is running
                return Ok(new
                {
                    success = true,
                    message = "Backend is healthy",
                    data = new
                    {
                        status = "healthy",
                        timestamp = DateTime.UtcNow,
                        version = "1.0.0"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Backend is unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Authenticated health check - verifies that authentication/authorization system is ready
        /// This endpoint requires a valid JWT token to pass, ensuring JWT middleware and policies are initialized
        /// </summary>
        [HttpGet("auth")]
        [Authorize(Policy = "AuthenticatedUser")]
        public IActionResult GetAuthHealth()
        {
            try
            {
                // This endpoint requires authentication, so if we get here, auth system is ready
                return Ok(new
                {
                    success = true,
                    message = "Authentication system is ready",
                    data = new
                    {
                        status = "healthy",
                        authReady = true,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Authentication system is not ready",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}

