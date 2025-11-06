using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CenterController : ControllerBase
    {
        private readonly ICenterService _centerService;
        private readonly IInventoryService _inventoryService;
        private static System.Collections.Generic.List<(int centerId, double lat, double lng)>? _geoCache;

        public CenterController(ICenterService centerService, IInventoryService inventoryService)
        {
            _centerService = centerService;
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Lấy danh sách tất cả trung tâm với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="city">Lọc theo thành phố</param>
        /// <returns>Danh sách trung tâm</returns>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllCenters(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? city = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _centerService.GetAllCentersAsync(pageNumber, pageSize, searchTerm, city);

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách trung tâm thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        // ========== Nearby Centers ==========
        public class NearbyQuery { public double lat { get; set; } public double lng { get; set; } public double radiusKm { get; set; } = 10; public int limit { get; set; } = 10; public int? serviceId { get; set; } = null; }

        [HttpGet("nearby")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearby([FromQuery] NearbyQuery q)
        {
            if (q.radiusKm <= 0 || q.radiusKm > 200) q.radiusKm = 10;
            if (q.limit <= 0 || q.limit > 50) q.limit = 10;
            if (q.lat < -90 || q.lat > 90 || q.lng < -180 || q.lng > 180)
                return BadRequest(new { success = false, message = "Toạ độ không hợp lệ" });

            // Load geo cache from file once
            if (_geoCache == null)
            {
                try
                {
                    var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "wwwroot", "center-geo.json");
                    if (!System.IO.File.Exists(path)) return Ok(new { success = true, data = System.Array.Empty<object>(), message = "Chưa cấu hình center-geo.json" });
                    var json = await System.IO.File.ReadAllTextAsync(path);
                    var arr = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<GeoItem>>(json) ?? new();
                    _geoCache = arr.Select(x => (x.centerId, x.lat, x.lng)).ToList();
                }
                catch { _geoCache = new(); }
            }

            var centers = await _centerService.GetActiveCentersAsync(1, int.MaxValue, null, null);
            var centerList = centers.Centers.Select(c => new { c.CenterId, c.CenterName, c.Address }).ToList();

            // Optional filter by serviceId could be added here using service layer if cần

            var deltaLat = q.radiusKm / 111d;
            var deltaLng = q.radiusKm / (111d * System.Math.Cos(q.lat * System.Math.PI / 180d));
            double minLat = q.lat - deltaLat, maxLat = q.lat + deltaLat, minLng = q.lng - deltaLng, maxLng = q.lng + deltaLng;

            var joined = from c in centerList
                         join g in _geoCache on c.CenterId equals g.centerId
                         where g.lat >= minLat && g.lat <= maxLat && g.lng >= minLng && g.lng <= maxLng
                         let dist = HaversineKm(q.lat, q.lng, g.lat, g.lng)
                         orderby dist ascending
                         select new { c.CenterId, name = c.CenterName, address = c.Address, distanceKm = System.Math.Round(dist, 2) };

            var data = joined.Take(q.limit).ToList();
            return Ok(new { success = true, data });
        }

        private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371d;
            double dLat = (lat2 - lat1) * System.Math.PI / 180d;
            double dLng = (lng2 - lng1) * System.Math.PI / 180d;
            double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) + System.Math.Cos(lat1 * System.Math.PI / 180d) * System.Math.Cos(lat2 * System.Math.PI / 180d) * System.Math.Sin(dLng / 2) * System.Math.Sin(dLng / 2);
            double c = 2 * System.Math.Asin(System.Math.Min(1, System.Math.Sqrt(a)));
            return R * c;
        }

        private class GeoItem { public int centerId { get; set; } public double lat { get; set; } public double lng { get; set; } }

        // ========== Nearest fulfillment (distance + stock) ==========
        public class NearestFulfillmentQuery { public double lat { get; set; } public double lng { get; set; } public string items { get; set; } = string.Empty; }

        [HttpGet("nearest-fulfillment")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearestFulfillment([FromQuery] NearestFulfillmentQuery q)
        {
            if (q.lat < -90 || q.lat > 90 || q.lng < -180 || q.lng > 180)
                return BadRequest(new { success = false, message = "Toạ độ không hợp lệ" });

            // Parse items: format "partId:qty,partId:qty"
            var parsed = new System.Collections.Generic.List<(int partId, int qty)>();
            foreach (var token in (q.items ?? string.Empty).Split(',', System.StringSplitOptions.RemoveEmptyEntries))
            {
                var p = token.Split(':', System.StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 2 && int.TryParse(p[0], out var pid) && int.TryParse(p[1], out var qty) && pid > 0 && qty > 0)
                    parsed.Add((pid, qty));
            }
            if (parsed.Count == 0)
                return BadRequest(new { success = false, message = "items rỗng hoặc không đúng định dạng partId:qty" });

            // Load active centers
            var centersPage = await _centerService.GetActiveCentersAsync(1, int.MaxValue, null, null);
            var centers = centersPage.Centers.Select(c => new { c.CenterId, c.CenterName }).ToList();

            // We need lat/lng from DB; fall back to geo file cache if service responses don't include it
            if (_geoCache == null)
            {
                try
                {
                    var path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "wwwroot", "center-geo.json");
                    if (System.IO.File.Exists(path))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(path);
                        var arr = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<GeoItem>>(json) ?? new();
                        _geoCache = arr.Select(x => (x.centerId, x.lat, x.lng)).ToList();
                    }
                    else
                    {
                        _geoCache = new();
                    }
                }
                catch { _geoCache = new(); }
            }

            // For each center: check stock via inventory service, then compute distance using geo cache
            var feasible = new System.Collections.Generic.List<(int centerId, string name, double distanceKm)>();
            foreach (var c in centers)
            {
                var geo = _geoCache.FirstOrDefault(g => g.centerId == c.CenterId);
                if (geo.centerId == 0) continue; // skip if no geo

                var inv = await _inventoryService.GetInventoryByCenterIdAsync(c.CenterId);
                var parts = inv.InventoryParts ?? new System.Collections.Generic.List<InventoryPartResponse>();
                bool ok = parsed.All(req =>
                    parts.Any(p => p.PartId == req.partId && (p.CurrentStock) >= req.qty));
                if (!ok) continue;

                var dist = HaversineKm(q.lat, q.lng, geo.lat, geo.lng);
                feasible.Add((c.CenterId, c.CenterName, dist));
            }

            var result = feasible.OrderBy(x => x.distanceKm).FirstOrDefault();
            if (result.centerId == 0)
                return Ok(new { success = true, data = (object?)null, message = "Không có trung tâm nào đủ hàng" });

            return Ok(new { success = true, data = new { centerId = result.centerId, name = result.name, distanceKm = System.Math.Round(result.distanceKm, 2) } });
        }

        /// <summary>
        /// Lấy danh sách trung tâm đang hoạt động với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="city">Lọc theo thành phố</param>
        /// <returns>Danh sách trung tâm đang hoạt động</returns>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCenters(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? city = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _centerService.GetActiveCentersAsync(pageNumber, pageSize, searchTerm, city);

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách trung tâm đang hoạt động thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin trung tâm theo ID
        /// </summary>
        /// <param name="id">ID trung tâm</param>
        /// <returns>Thông tin trung tâm</returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCenterById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                var center = await _centerService.GetCenterByIdAsync(id);

                return Ok(new {
                    success = true,
                    message = "Lấy thông tin trung tâm thành công",
                    data = center
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Tạo trung tâm mới
        /// </summary>
        /// <param name="request">Thông tin trung tâm mới</param>
        /// <returns>Thông tin trung tâm đã tạo</returns>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateCenter([FromBody] CreateCenterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                }

                var center = await _centerService.CreateCenterAsync(request);

                return CreatedAtAction(nameof(GetCenterById), new { id = center.CenterId }, new {
                    success = true,
                    message = "Tạo trung tâm thành công",
                    data = center
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin trung tâm
        /// </summary>
        /// <param name="id">ID trung tâm</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin trung tâm đã cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateCenter(int id, [FromBody] UpdateCenterRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                }

                var center = await _centerService.UpdateCenterAsync(id, request);

                return Ok(new {
                    success = true,
                    message = "Cập nhật thông tin trung tâm thành công",
                    data = center
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

    }
}
