using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.Api.Controllers
{
[ApiController]
[Route("api/workorders/{workOrderId:int}/parts")]
[Authorize]
public class WorkOrderPartsController : ControllerBase
    {
        private readonly IWorkOrderPartRepository _repo;
        public WorkOrderPartsController(IWorkOrderPartRepository repo) { _repo = repo; }

        [HttpGet]
        public async Task<IActionResult> Get(int workOrderId)
        {
            var items = await _repo.GetByWorkOrderIdAsync(workOrderId);
            var result = items.Select(x => new
            {
                partId = x.PartId,
                partName = x.Part?.PartName,
                quantity = x.QuantityUsed,
                unitPrice = x.UnitCost,
                total = x.UnitCost * x.QuantityUsed
            });
            return Ok(new { success = true, data = result });
        }

        public class AddRequest { public int PartId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } public string Note { get; set; } }

        [HttpPost]
        public async Task<IActionResult> Add(int workOrderId, [FromBody] AddRequest req)
        {
            var item = new WorkOrderPart
            {
                WorkOrderId = workOrderId,
                PartId = req.PartId,
                QuantityUsed = req.Quantity,
                UnitCost = req.UnitPrice
            };
            await _repo.AddAsync(item);
            return Ok(new { success = true });
        }

        public class UpdateRequest { public int Quantity { get; set; } public decimal UnitPrice { get; set; } }

        [HttpPut("{partId:int}")]
        public async Task<IActionResult> Update(int workOrderId, int partId, [FromBody] UpdateRequest req)
        {
            var item = new WorkOrderPart
            {
                WorkOrderId = workOrderId,
                PartId = partId,
                QuantityUsed = req.Quantity,
                UnitCost = req.UnitPrice
            };
            await _repo.UpdateAsync(item);
            return Ok(new { success = true });
        }

        [HttpDelete("{partId:int}")]
        public async Task<IActionResult> Delete(int workOrderId, int partId)
        {
            await _repo.DeleteAsync(workOrderId, partId);
            return Ok(new { success = true });
        }
    }
}


