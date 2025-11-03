using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.Api.Controllers
{
[ApiController]
[Route("api/bookings/{bookingId:int}/parts")]
[Authorize]
public class WorkOrderPartsController : ControllerBase
    {
        private readonly IWorkOrderPartRepository _repo;
        private readonly IVehicleModelPartRepository _modelParts;
        private readonly IPartRepository _partRepo;
        private readonly IBookingRepository _bookingRepo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<EVServiceCenter.Api.BookingHub> _hub;
        public WorkOrderPartsController(IWorkOrderPartRepository repo, IVehicleModelPartRepository modelParts, IPartRepository partRepo, IBookingRepository bookingRepo, IInventoryRepository inventoryRepo, Microsoft.AspNetCore.SignalR.IHubContext<EVServiceCenter.Api.BookingHub> hub) { _repo = repo; _modelParts = modelParts; _partRepo = partRepo; _bookingRepo = bookingRepo; _inventoryRepo = inventoryRepo; _hub = hub; }

        [HttpGet]
        public async Task<IActionResult> Get(int bookingId)
        {
            var items = await _repo.GetByBookingIdAsync(bookingId);
            var result = items.Select(x => new
            {
                partId = x.PartId,
                partName = x.Part?.PartName,
                quantity = x.QuantityUsed,
                unitPrice = x.Part?.Price ?? 0,
                total = (x.Part?.Price ?? 0) * x.QuantityUsed
            });
            return Ok(new { success = true, data = result });
        }

        public class AddRequest { public int PartId { get; set; } public int Quantity { get; set; } public string? Note { get; set; } }

        [HttpPost]
        public async Task<IActionResult> Add(int bookingId, [FromBody] AddRequest req)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            if (string.Equals(booking.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(booking.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi booking đã hoàn tất/hủy" });

            var item = new WorkOrderPart
            {
                BookingId = bookingId,
                PartId = req.PartId,
                QuantityUsed = req.Quantity
            };
            await _repo.AddAsync(item);
            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("parts.updated", new object[] { new { bookingId, type = "add", partId = req.PartId, quantity = req.Quantity } });
            return Ok(new { success = true });
        }

        public class UpdateRequest { public int Quantity { get; set; } }

        [HttpPut("{partId:int}")]
        public async Task<IActionResult> Update(int bookingId, int partId, [FromBody] UpdateRequest req)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            if (string.Equals(booking.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(booking.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi booking đã hoàn tất/hủy" });

            var item = new WorkOrderPart
            {
                BookingId = bookingId,
                PartId = partId,
                QuantityUsed = req.Quantity
            };
            await _repo.UpdateAsync(item);
            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("parts.updated", new object[] { new { bookingId, type = "update", partId, quantity = req.Quantity } });
            return Ok(new { success = true });
        }

        [HttpDelete("{partId:int}")]
        public async Task<IActionResult> Delete(int bookingId, int partId)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            if (string.Equals(booking.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(booking.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi booking đã hoàn tất/hủy" });
            await _repo.DeleteAsync(bookingId, partId);
            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("parts.updated", new object[] { new { bookingId, type = "delete", partId } });
            return Ok(new { success = true });
        }

        public class WorkOrderPartBulkItem { public int PartId { get; set; } public int Quantity { get; set; } }
        public class WorkOrderPartBulkRequest { public System.Collections.Generic.List<WorkOrderPartBulkItem> Items { get; set; } = new(); }

        [HttpPost("bulk")]
        public async Task<IActionResult> UpsertBulk(int bookingId, [FromBody] WorkOrderPartBulkRequest req)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            if (string.Equals(booking.Status, "COMPLETED", System.StringComparison.OrdinalIgnoreCase) || string.Equals(booking.Status, "CANCELED", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Không thể sửa parts khi booking đã hoàn tất/hủy" });
            var items = (req?.Items ?? new System.Collections.Generic.List<WorkOrderPartBulkItem>());
            foreach (var i in items)
            {
                var entity = new WorkOrderPart { BookingId = bookingId, PartId = i.PartId, QuantityUsed = i.Quantity };
                await _repo.UpdateAsync(entity);
            }
            return Ok(new { success = true, count = items.Count });
        }

        public class ConfirmRequest { public int? CenterId { get; set; } }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int bookingId, [FromBody] ConfirmRequest? req)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            var status = (booking.Status ?? string.Empty).ToUpperInvariant();
            if (status == "COMPLETED" || status == "CANCELED" || status == "CANCELLED")
                return BadRequest(new { success = false, message = "Không thể xác nhận parts khi booking đã hoàn tất/hủy" });

            var centerId = req?.CenterId ?? booking.CenterId;
            var inventory = await _inventoryRepo.GetInventoryByCenterIdAsync(centerId);
            if (inventory == null) return BadRequest(new { success = false, message = "Trung tâm chưa có kho" });

            var items = await _repo.GetByBookingIdAsync(bookingId);
            if (items == null || items.Count == 0) return BadRequest(new { success = false, message = "Không có phụ tùng phát sinh để xác nhận" });

            // Tính tổng theo PartId
            var sumByPart = items
                .GroupBy(i => i.PartId)
                .Select(g => new { PartId = g.Key, Quantity = g.Sum(x => x.QuantityUsed) })
                .ToList();

            // Kiểm tra tồn kho đủ
            foreach (var s in sumByPart)
            {
                var invPart = await _inventoryRepo.GetInventoryPartByInventoryAndPartAsync(inventory.InventoryId, s.PartId);
                if (invPart == null)
                    return BadRequest(new { success = false, message = $"Phụ tùng {s.PartId} không có trong kho trung tâm" });
                if (invPart.CurrentStock < s.Quantity)
                    return BadRequest(new { success = false, message = $"Không đủ tồn kho cho phụ tùng {s.PartId}. Còn {invPart.CurrentStock}, cần {s.Quantity}" });
            }

            // Trừ kho
            foreach (var s in sumByPart)
            {
                var invPart = await _inventoryRepo.GetInventoryPartByInventoryAndPartAsync(inventory.InventoryId, s.PartId);
                if (invPart == null)
                    return BadRequest(new { success = false, message = $"Phụ tùng {s.PartId} không có trong kho trung tâm" });
                invPart.CurrentStock -= s.Quantity;
                if (invPart.CurrentStock < 0) invPart.CurrentStock = 0; // safety
                invPart.LastUpdated = System.DateTime.UtcNow;
                await _inventoryRepo.UpdateInventoryPartAsync(invPart);
            }

            inventory.LastUpdated = System.DateTime.UtcNow;
            await _inventoryRepo.UpdateInventoryAsync(inventory);

            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("parts.updated", new object[] { new { bookingId, type = "confirm", centerId } });
            return Ok(new { success = true, message = "Đã xác nhận phụ tùng phát sinh và trừ kho theo trung tâm", centerId, items = sumByPart });
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> Suggestions(int bookingId, [FromQuery] int? serviceId = null, [FromQuery] int? modelId = null)
        {
            var list = new System.Collections.Generic.List<object>();
            var servicePartIds = new System.Collections.Generic.HashSet<int>();
            // ServiceParts removed: only suggest by model compatibility when provided
            if (modelId.HasValue && modelId.Value > 0)
            {
                var compatibles = await _modelParts.GetCompatiblePartsByModelIdAsync(modelId.Value);
                foreach (var mp in compatibles)
                {
                    list.Add(new { partId = mp.PartId, vehicleModelPartId = mp.Id });
                }
            }
            return Ok(new { success = true, data = list });
        }
    }
}


