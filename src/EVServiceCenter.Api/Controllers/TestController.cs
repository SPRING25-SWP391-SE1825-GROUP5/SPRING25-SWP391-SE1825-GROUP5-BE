using System.Linq;
using EVServiceCenter.Domain.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly DbContext _context;

        public TestController(EVDbContext context)
        {
            _context = context;
        }

        [HttpGet("tables")]
        public IActionResult GetTables()
        {
            var tables = _context.Model.GetEntityTypes()
                           .Select(e => e.GetTableName())
                           .ToList();

            return Ok(tables);
        }
    }
}
