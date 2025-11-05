using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/parts")]
    public class PartsController : ControllerBase
    {
        private readonly IPartRepository _partRepository;

        public PartsController(IPartRepository partRepository)
        {
            _partRepository = partRepository;
        }

        [HttpGet("by-category/{categoryId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return BadRequest(new { success = false, message = "CategoryId không hợp lệ" });
            }

            var parts = await _partRepository.GetPartsByCategoryIdAsync(categoryId);

            var data = parts.Select(p => new
            {
                p.PartId,
                p.PartNumber,
                p.PartName,
                p.Brand,
                p.Price,
                p.ImageUrl,
                p.IsActive
            });

            return Ok(new { success = true, data });
        }
    }
}


