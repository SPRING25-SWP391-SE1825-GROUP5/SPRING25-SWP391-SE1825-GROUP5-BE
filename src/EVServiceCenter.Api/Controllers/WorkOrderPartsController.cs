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
        private readonly IVehicleModelPartRepository _modelParts;
        private readonly IPartRepository _partRepo;
        private readonly IWorkOrderRepository _woRepo;
        public WorkOrderPartsController(IWorkOrderPartRepository repo, IVehicleModelPartRepository modelParts, IPartRepository partRepo, IWorkOrderRepository woRepo) { _repo = repo; _modelParts = modelParts; _partRepo = partRepo; _woRepo = woRepo; }

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
            var wo = await _woRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (string.Equals(wo.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(wo.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi work order đã hoàn tất/hủy" });

            var unit = req.UnitPrice;
            if (unit <= 0)
            {
                var part = await _partRepo.GetPartLiteByIdAsync(req.PartId);
                unit = part?.Price ?? 0;
            }
            var item = new WorkOrderPart
            {
                WorkOrderId = workOrderId,
                PartId = req.PartId,
                QuantityUsed = req.Quantity,
                UnitCost = unit
            };
            await _repo.AddAsync(item);
            return Ok(new { success = true });
        }

        public class UpdateRequest { public int Quantity { get; set; } public decimal UnitPrice { get; set; } }

        [HttpPut("{partId:int}")]
        public async Task<IActionResult> Update(int workOrderId, int partId, [FromBody] UpdateRequest req)
        {
            var wo = await _woRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (string.Equals(wo.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(wo.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi work order đã hoàn tất/hủy" });

            var unit = req.UnitPrice;
            if (unit <= 0)
            {
                var part = await _partRepo.GetPartLiteByIdAsync(partId);
                unit = part?.Price ?? 0;
            }
            var item = new WorkOrderPart
            {
                WorkOrderId = workOrderId,
                PartId = partId,
                QuantityUsed = req.Quantity,
                UnitCost = unit
            };
            await _repo.UpdateAsync(item);
            return Ok(new { success = true });
        }

        [HttpDelete("{partId:int}")]
        public async Task<IActionResult> Delete(int workOrderId, int partId)
        {
            var wo = await _woRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (string.Equals(wo.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(wo.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi work order đã hoàn tất/hủy" });
            await _repo.DeleteAsync(workOrderId, partId);
            return Ok(new { success = true });
        }

        public class WorkOrderPartBulkItem { public int PartId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
        public class WorkOrderPartBulkRequest { public System.Collections.Generic.List<WorkOrderPartBulkItem> Items { get; set; } }

        [HttpPost("bulk")]
        public async Task<IActionResult> UpsertBulk(int workOrderId, [FromBody] WorkOrderPartBulkRequest req)
        {
            var wo = await _woRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (string.Equals(wo.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(wo.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi work order đã hoàn tất/hủy" });
            var items = (req?.Items ?? new System.Collections.Generic.List<WorkOrderPartBulkItem>());
            foreach (var i in items)
            {
                var unit = i.UnitPrice;
                if (unit <= 0)
                {
                    var part = await _partRepo.GetPartLiteByIdAsync(i.PartId);
                    unit = part?.Price ?? 0;
                }
                var entity = new WorkOrderPart { WorkOrderId = workOrderId, PartId = i.PartId, QuantityUsed = i.Quantity, UnitCost = unit };
                await _repo.UpdateAsync(entity);
            }
            return Ok(new { success = true, count = items.Count });
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> Suggestions(int workOrderId, [FromQuery] int? serviceId = null, [FromQuery] int? modelId = null)
        {
            var list = new System.Collections.Generic.List<object>();
            var servicePartIds = new System.Collections.Generic.HashSet<int>();
            // ServiceParts removed: only suggest by model compatibility when provided
            if (modelId.HasValue && modelId.Value > 0)
            {
                var compatibles = await _modelParts.GetCompatiblePartsByModelIdAsync(modelId.Value);
                foreach (var mp in compatibles)
                {
                    list.Add(new { partId = mp.PartId, vehicleModelPartId = mp.Id, isCompatible = mp.IsCompatible });
                }
            }
            return Ok(new { success = true, data = list });
        }
    }
}


