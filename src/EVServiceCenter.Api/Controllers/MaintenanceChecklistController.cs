using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/workorders/{workOrderId:int}/checklist")]
    public class MaintenanceChecklistController : ControllerBase
    {
        private readonly IMaintenanceChecklistRepository _checkRepo;
        private readonly IMaintenanceChecklistResultRepository _resultRepo;
        private readonly IWorkOrderRepository _workOrderRepo;
        private readonly IServicePartRepository _servicePartRepo;

        public MaintenanceChecklistController(
            IMaintenanceChecklistRepository checkRepo,
            IMaintenanceChecklistResultRepository resultRepo,
            IWorkOrderRepository workOrderRepo,
            IServicePartRepository servicePartRepo)
        {
            _checkRepo = checkRepo;
            _resultRepo = resultRepo;
            _workOrderRepo = workOrderRepo;
            _servicePartRepo = servicePartRepo;
        }

        // POST /api/workorders/{id}/checklist/init
        [HttpPost("init")]
        public async Task<IActionResult> Init(int workOrderId)
        {
            var workOrder = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (workOrder == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });

            var existing = await _checkRepo.GetByWorkOrderIdAsync(workOrderId);
            if (existing != null) return Ok(new { success = true, checklistId = existing.ChecklistId });

            var checklist = await _checkRepo.CreateAsync(new MaintenanceChecklist
            {
                WorkOrderId = workOrderId,
                CreatedAt = DateTime.UtcNow,
                Notes = null
            });

            // Init từ ServiceParts: mỗi Part là một mục kiểm tra
            var serviceParts = await _servicePartRepo.GetByServiceIdAsync(workOrder.Booking?.ServiceId ?? 0);
            var results = serviceParts.Select(sp => new MaintenanceChecklistResult
            {
                ChecklistId = checklist.ChecklistId,
                PartId = sp.PartId,
                Description = sp.Notes ?? sp.Part?.PartName,
                IsMandatory = true,
                Performed = false,
                Result = null,
                Comment = null
            });
            await _resultRepo.UpsertManyAsync(results);

            return Ok(new { success = true, checklistId = checklist.ChecklistId });
        }

        // GET /api/workorders/{id}/checklist
        [HttpGet]
        public async Task<IActionResult> Get(int workOrderId)
        {
            var checklist = await _checkRepo.GetByWorkOrderIdAsync(workOrderId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var data = results.Select(r => new
            {
                resultId = r.ResultId,
                partId = r.PartId,
                partName = r.Part?.PartName,
                description = r.Description,
                isMandatory = r.IsMandatory,
                performed = r.Performed,
                result = r.Result,
                note = r.Comment
            });
            return Ok(new { success = true, checklistId = checklist.ChecklistId, items = data });
        }

        public class UpdateItemRequest { public string Description { get; set; } public bool IsMandatory { get; set; } public bool Performed { get; set; } public string Result { get; set; } public string Note { get; set; } }

        // PUT /api/workorders/{id}/checklist/{resultId}
        [HttpPut("{resultId:int}")]
        public async Task<IActionResult> UpdateItem(int workOrderId, int resultId, [FromBody] UpdateItemRequest req)
        {
            var checklist = await _checkRepo.GetByWorkOrderIdAsync(workOrderId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            await _resultRepo.UpsertAsync(new MaintenanceChecklistResult
            {
                ResultId = resultId,
                ChecklistId = checklist.ChecklistId,
                Description = req.Description,
                IsMandatory = req.IsMandatory,
                Performed = req.Performed,
                Result = req.Result,
                Comment = req.Note
            });
            return Ok(new { success = true });
        }

        public class BulkRequest { public System.Collections.Generic.List<BulkItem> Items { get; set; } public class BulkItem { public int? ResultId { get; set; } public string Description { get; set; } public bool IsMandatory { get; set; } public bool Performed { get; set; } public string Result { get; set; } public string Note { get; set; } } }

        // PUT /api/workorders/{id}/checklist
        [HttpPut]
        public async Task<IActionResult> UpdateBulk(int workOrderId, [FromBody] BulkRequest req)
        {
            var checklist = await _checkRepo.GetByWorkOrderIdAsync(workOrderId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var incoming = (req.Items ?? new System.Collections.Generic.List<BulkRequest.BulkItem>()).ToList();
            var results = incoming.Select(i => new MaintenanceChecklistResult
            {
                ResultId = i.ResultId ?? 0,
                ChecklistId = checklist.ChecklistId,
                Description = i.Description,
                IsMandatory = i.IsMandatory,
                Performed = i.Performed,
                Result = i.Result,
                Comment = i.Note
            });
            await _resultRepo.UpsertManyAsync(results);
            return Ok(new { success = true });
        }

        // GET /api/workorders/{id}/checklist/summary
        [HttpGet("summary")]
        public async Task<IActionResult> Summary(int workOrderId)
        {
            var checklist = await _checkRepo.GetByWorkOrderIdAsync(workOrderId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var pass = results.Count(r => string.Equals(r.Result, "PASS", StringComparison.OrdinalIgnoreCase));
            var fail = results.Count(r => string.Equals(r.Result, "FAIL", StringComparison.OrdinalIgnoreCase));
            var na = results.Count(r => string.Equals(r.Result, "NA", StringComparison.OrdinalIgnoreCase));
            var performed = results.Count(r => r.Performed);
            var total = results.Count;
            return Ok(new { success = true, checklistId = checklist.ChecklistId, total, performed, pass, fail, na });
        }
    }
}


