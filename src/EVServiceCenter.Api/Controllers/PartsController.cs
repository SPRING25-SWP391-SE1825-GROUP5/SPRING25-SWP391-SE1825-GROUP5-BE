using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/parts")]
    public class PartsController : ControllerBase
    {
        private readonly IPartRepository _partRepository;
        private readonly IInventoryService _inventoryService;

        public PartsController(IPartRepository partRepository, IInventoryService inventoryService)
        {
            _partRepository = partRepository;
            _inventoryService = inventoryService;
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

        [HttpGet("by-category/{categoryId:int}/with-inventory")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategoryWithInventory(int categoryId, [FromQuery] int? centerId = null)
        {
            if (categoryId <= 0)
            {
                return BadRequest(new { success = false, message = "CategoryId không hợp lệ" });
            }

            var parts = await _partRepository.GetPartsByCategoryIdAsync(categoryId);
            var partIds = parts.Select(p => p.PartId).ToList();

            // Build stock map either per-center or global
            var stockByPartId = new Dictionary<int, int>();

            if (centerId.HasValue && centerId.Value > 0)
            {
                var inv = await _inventoryService.GetInventoryByCenterIdAsync(centerId.Value);
                if (inv.InventoryParts != null && inv.InventoryParts.Count > 0)
                {
                    foreach (var ip in inv.InventoryParts.Where(ip => partIds.Contains(ip.PartId)))
                    {
                        stockByPartId[ip.PartId] = ip.CurrentStock;
                    }
                }
            }
            else
            {
                var global = await _inventoryService.GetGlobalAvailabilityAsync(partIds);
                foreach (var g in global)
                {
                    stockByPartId[g.PartId] = g.TotalStock;
                }
            }

            var data = parts.Select(p => new
            {
                p.PartId,
                p.PartNumber,
                p.PartName,
                p.Brand,
                p.Price,
                p.ImageUrl,
                p.IsActive,
                stock = stockByPartId.TryGetValue(p.PartId, out var s) ? s : 0
            });

            return Ok(new { success = true, data });
        }
    }
}


