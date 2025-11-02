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
    }
}

