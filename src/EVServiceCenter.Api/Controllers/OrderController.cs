using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;
using System.IO;
using Microsoft.Extensions.Configuration;
using EVServiceCenter.Api.Constants;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedUser")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly PaymentService _paymentService;
    private readonly IOrderHistoryService _orderHistoryService;
    private readonly IOptions<ExportOptions> _exportOptions;
    private readonly IConfiguration _configuration;
    private readonly EVServiceCenter.Domain.Interfaces.IInventoryRepository _inventoryRepository;

    public OrderController(IOrderService orderService, PaymentService paymentService, IOrderHistoryService orderHistoryService, IOptions<ExportOptions> exportOptions, IConfiguration configuration, EVServiceCenter.Domain.Interfaces.IInventoryRepository inventoryRepository)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _orderHistoryService = orderHistoryService;
        _exportOptions = exportOptions;
        _configuration = configuration;
        _inventoryRepository = inventoryRepository;
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId)
    {
        try
        {
            var orders = await _orderService.GetByCustomerIdAsync(customerId);
            return Ok(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("customer/{customerId}/available-for-booking")]
    public async Task<IActionResult> GetAvailableOrdersForBooking(int customerId, [FromQuery] int? centerId = null)
    {
        try
        {
            if (!centerId.HasValue)
                return BadRequest(new { success = false, message = "centerId là bắt buộc" });

            var allOrders = await _orderService.GetByCustomerIdAsync(customerId);
            var paidOrders = allOrders.Where(o => o.Status == "PAID").ToList();

            if (paidOrders.Count == 0)
                return Ok(new { success = true, data = new List<object>() });

            var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId.Value);
            if (inventory == null)
                return BadRequest(new { success = false, message = $"Không tìm thấy kho của chi nhánh {centerId.Value}" });

            var inventoryParts = inventory.InventoryParts ?? new List<InventoryPart>();

            var result = new List<object>();

            foreach (var order in paidOrders)
            {
                var orderItems = await _orderService.GetItemsAsync(order.OrderId);
                var availableItems = new List<object>();

                foreach (var oi in orderItems)
                {
                    if (oi.AvailableQty <= 0)
                        continue;

                    if (order.FulfillmentCenterId != centerId.Value)
                        continue;

                    var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == oi.PartId);
                    if (invPart == null)
                    {
                        availableItems.Add(new
                        {
                            orderItemId = oi.OrderItemId,
                            partId = oi.PartId,
                            partName = oi.PartName,
                            availableQty = oi.AvailableQty,
                            unitPrice = oi.UnitPrice,
                            canUse = false,
                            warning = "Phụ tùng không có trong kho của chi nhánh này"
                        });
                        continue;
                    }

                    var canUse = invPart.ReservedQty >= oi.AvailableQty && invPart.CurrentStock >= oi.AvailableQty;

                    availableItems.Add(new
                    {
                        orderItemId = oi.OrderItemId,
                        partId = oi.PartId,
                        partName = oi.PartName,
                        availableQty = oi.AvailableQty,
                        unitPrice = oi.UnitPrice,
                        canUse = canUse,
                        warning = canUse ? null : $"ReservedQty ({invPart.ReservedQty}) hoặc CurrentStock ({invPart.CurrentStock}) không đủ"
                    });
                }

                if (availableItems.Count > 0)
                {
                    result.Add(new
                    {
                        orderId = order.OrderId,
                        orderNumber = order.OrderNumber,
                        totalAmount = order.TotalAmount,
                        createdAt = order.CreatedAt,
                        fulfillmentCenterId = order.FulfillmentCenterId,
                        fulfillmentCenterName = order.FulfillmentCenterName,
                        availableParts = availableItems
                    });
                }
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{orderId}/items")]
    public async Task<IActionResult> GetItems(int orderId)
    {
        try
        {
            var items = await _orderService.GetItemsAsync(orderId);
            return Ok(new { success = true, data = items });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi lấy danh sách item: " + ex.Message });
        }
    }

    [HttpPost("{orderId}/checkout/online")]
    public async Task<IActionResult> CheckoutOnline(int orderId)
    {
        try
        {
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            var orderCancelUrl = $"{frontendUrl}/api/payment/order/{orderId}/cancel";

            var url = await _paymentService.CreateOrderPaymentLinkAsync(orderId, orderCancelUrl);
            return Ok(new {
                success = true,
                checkoutUrl = url,
                orderId = orderId,
                message = "Tạo payment link thành công"
            });
        }
        catch (ArgumentException ex)
        {
            // Validation lỗi: orderId không hợp lệ
            return BadRequest(new {
                success = false,
                message = ex.Message,
                errorType = ApiConstants.ErrorTypes.ValidationError,
                orderId = orderId
            });
        }
        catch (InvalidOperationException ex)
        {
            // Business logic lỗi: order không tồn tại, đã hủy, đã thanh toán, v.v.
            var message = ex.Message;

            // Phân loại lỗi dựa trên message để trả về status code phù hợp
            if (message.Contains("không tồn tại") || message.Contains("Không tìm thấy"))
            {
                return NotFound(new {
                    success = false,
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderNotFound,
                    orderId = orderId
                });
            }
            else if (message.Contains("đã bị hủy") || message.Contains("đã được thanh toán") || message.Contains("hóa đơn thanh toán"))
            {
                return BadRequest(new {
                    success = false,
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderInvalidState,
                    orderId = orderId
                });
            }
            else
            {
                return BadRequest(new {
                    success = false,
                    message = message,
                    errorType = ApiConstants.ErrorTypes.BusinessRuleViolation,
                    orderId = orderId
                });
            }
        }
        catch (Exception ex)
        {
            // Lỗi hệ thống không mong đợi
            return StatusCode(500, new {
                success = false,
                message = $"Lỗi hệ thống khi tạo payment link: {ex.Message}",
                errorType = ApiConstants.ErrorTypes.SystemError,
                orderId = orderId
            });
        }
    }

    [HttpGet("{orderId}/payment/link")]
    public async Task<IActionResult> GetOrderPaymentLink(int orderId)
    {
        try
        {
            var checkoutUrl = await _paymentService.GetExistingOrderPaymentLinkAsync(orderId);
            if (string.IsNullOrEmpty(checkoutUrl))
            {
                return NotFound(new {
                    success = false,
                    message = $"Không tìm thấy payment link cho đơn hàng #{orderId}. Payment link có thể chưa được tạo hoặc đã hết hạn.",
                    orderId = orderId
                });
            }

            return Ok(new {
                success = true,
                checkoutUrl = checkoutUrl,
                orderCode = orderId,
                message = "Lấy payment link thành công"
            });
        }
        catch (ArgumentException ex)
        {
            // Validation lỗi: orderId không hợp lệ
            return BadRequest(new {
                success = false,
                message = ex.Message,
                errorType = ApiConstants.ErrorTypes.ValidationError,
                orderId = orderId
            });
        }
        catch (InvalidOperationException ex)
        {
            // Business logic lỗi: order không tồn tại, đã hủy, đã thanh toán, v.v.
            var message = ex.Message;

            // Phân loại lỗi dựa trên message để trả về status code phù hợp
            if (message.Contains("không tồn tại") || message.Contains("Không tìm thấy"))
            {
                return NotFound(new {
                    success = false,
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderNotFound,
                    orderId = orderId
                });
            }
            else if (message.Contains("đã bị hủy") || message.Contains("đã được thanh toán") || message.Contains("hóa đơn thanh toán"))
            {
                return BadRequest(new {
                    success = false,
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderInvalidState,
                    orderId = orderId
                });
            }
            else
            {
                return BadRequest(new {
                    success = false,
                    message = message,
                    errorType = ApiConstants.ErrorTypes.BusinessRuleViolation,
                    orderId = orderId
                });
            }
        }
        catch (Exception ex)
        {
            // Lỗi hệ thống không mong đợi
            return StatusCode(500, new {
                success = false,
                message = $"Lỗi hệ thống khi lấy payment link: {ex.Message}",
                errorType = ApiConstants.ErrorTypes.SystemError,
                orderId = orderId
            });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetById(int orderId)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Đơn hàng không tồn tại" });

            return Ok(new { success = true, data = order });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{orderId}/fulfillment-center")]
    public async Task<IActionResult> UpdateFulfillmentCenter(int orderId, [FromBody] UpdateFulfillmentCenterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            var order = await _orderService.UpdateFulfillmentCenterAsync(orderId, request);
            return Ok(new { success = true, data = order, message = "Đã cập nhật chi nhánh thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ExportOrders()
    {
        try
        {
            var opts = _exportOptions.Value;
            var orders = await _orderService.GetAllAsync()
                ?? new System.Collections.Generic.List<EVServiceCenter.Application.Models.Responses.OrderResponse>();
            var total = orders.Count;
            if (total > opts.MaxRecords)
            {
                return BadRequest(new { success = false, message = $"Số bản ghi ({total}) vượt quá giới hạn cho phép ({opts.MaxRecords}). Vui lòng thu hẹp bộ lọc." });
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var bytes = GenerateOrdersXlsx(orders, opts.DateFormat);
            var fileName = $"orders_{timestamp}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private static byte[] GenerateOrdersXlsx(System.Collections.Generic.IList<EVServiceCenter.Application.Models.Responses.OrderResponse> orders, string dateFormat)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("Orders");

        var headers = new[] { "OrderId", "OrderNumber", "CustomerId", "CustomerName", "CustomerPhone", "TotalAmount", "Status", "CreatedAt", "UpdatedAt" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
        ws.SheetView.FreezeRows(1);

        int r = 2;
        foreach (var o in orders)
        {
            ws.Cell(r, 1).Value = o.OrderId;
            ws.Cell(r, 2).Value = o.OrderNumber ?? string.Empty;
            ws.Cell(r, 3).Value = o.CustomerId;
            ws.Cell(r, 4).Value = o.CustomerName ?? string.Empty;
            ws.Cell(r, 5).Value = o.CustomerPhone ?? string.Empty;
            ws.Cell(r, 6).Value = o.TotalAmount;
            ws.Cell(r, 7).Value = o.Status ?? string.Empty;
            ws.Cell(r, 8).Value = o.CreatedAt;
            ws.Cell(r, 9).Value = o.UpdatedAt;
            r++;
        }

        int lastRow = r - 1;
        int lastCol = headers.Length;

        ws.Range(2, 6, lastRow, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Range(2, 8, lastRow, 9).Style.DateFormat.Format = dateFormat;
        ws.Range(2, 7, lastRow, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        ws.Range(1, 1, lastRow, lastCol).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

        var tableRange = ws.Range(1, 1, lastRow, lastCol);
        var table = tableRange.CreateTable();
        table.Theme = ClosedXML.Excel.XLTableTheme.TableStyleMedium9;
        table.ShowAutoFilter = true;
        ws.Range(1, 1, lastRow, lastCol).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        ws.Range(1, 1, lastRow, lastCol).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    [HttpPost("customer/{customerId}/create")]
    public async Task<IActionResult> CreateOrderForCustomer(int customerId, [FromBody] CreateOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request == null)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            request.CustomerId = customerId;
            var order = await _orderService.CreateOrderAsync(request);
            return Ok(new { success = true, data = order, message = "Đã tạo đơn hàng thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("customers/{customerId}/orders/quick")]
    public async Task<IActionResult> CreateQuickOrder(int customerId, [FromBody] QuickOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request == null)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            request.CustomerId = customerId;
            var order = await _orderService.CreateQuickOrderAsync(request);
            return Ok(new { success = true, data = order, message = "Đã tạo đơn hàng mua ngay thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(orderId, request);
            return Ok(new { success = true, data = order, message = "Đã cập nhật trạng thái đơn hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi cập nhật trạng thái đơn hàng: " + ex.Message });
        }
    }

    [HttpDelete("{orderId}")]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        try
        {
            await _orderService.DeleteOrderAsync(orderId);
            return Ok(new { success = true, message = "Đã xóa đơn hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message });
        }
    }

    [HttpGet("/api/payment/order/{orderId}/cancel")]
    [AllowAnonymous]
    public IActionResult CancelOrderPayment([FromRoute] int orderId, [FromQuery] string? status = null, [FromQuery] string? code = null)
    {
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var frontendCancelUrl = $"{frontendUrl}/payment-cancel?orderId={orderId}&status={status}&code={code}";
        return Redirect(frontendCancelUrl);
    }

    [HttpGet("{orderId}/available-parts")]
    public async Task<IActionResult> GetAvailableParts(int orderId, [FromQuery] int? centerId = null)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Đơn hàng không tồn tại" });

            if (order.Status != "PAID")
                return BadRequest(new { success = false, message = "Đơn hàng chưa thanh toán" });

            var orderItems = await _orderService.GetItemsAsync(orderId);

            var targetCenterId = centerId ?? order.FulfillmentCenterId;
            if (!targetCenterId.HasValue)
            {
                return BadRequest(new { success = false, message = "Không xác định được chi nhánh để kiểm tra kho" });
            }

            var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(targetCenterId.Value);
            if (inventory == null)
            {
                return BadRequest(new { success = false, message = $"Không tìm thấy kho của chi nhánh {targetCenterId.Value}" });
            }

            var inventoryParts = inventory.InventoryParts ?? new List<InventoryPart>();

            var availableItems = new List<object>();

            foreach (var oi in orderItems)
            {
                if (oi.AvailableQty <= 0)
                    continue;

                if (centerId.HasValue && order.FulfillmentCenterId != centerId.Value)
                    continue;

                var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == oi.PartId);
                if (invPart == null)
                {
                    availableItems.Add(new
                    {
                        orderItemId = oi.OrderItemId,
                        partId = oi.PartId,
                        partName = oi.PartName,
                        quantity = oi.Quantity,
                        consumedQty = oi.ConsumedQty,
                        availableQty = oi.AvailableQty,
                        unitPrice = oi.UnitPrice,
                        subtotal = oi.Subtotal,
                        fulfillmentCenterId = order.FulfillmentCenterId,
                        fulfillmentCenterName = order.FulfillmentCenterName,
                        currentStock = (int?)null,
                        reservedQty = (int?)null,
                        inventoryAvailableQty = (int?)null,
                        canUse = false,
                        warning = "Phụ tùng không có trong kho của chi nhánh này"
                    });
                    continue;
                }

                var inventoryAvailableQty = invPart.CurrentStock - invPart.ReservedQty;
                var reservedQtyForThisItem = invPart.ReservedQty;
                var canUse = reservedQtyForThisItem >= oi.AvailableQty && invPart.CurrentStock >= oi.AvailableQty;

                availableItems.Add(new
                {
                    orderItemId = oi.OrderItemId,
                    partId = oi.PartId,
                    partName = oi.PartName,
                    quantity = oi.Quantity,
                    consumedQty = oi.ConsumedQty,
                    availableQty = oi.AvailableQty,
                        unitPrice = oi.UnitPrice,
                        subtotal = oi.Subtotal,
                        fulfillmentCenterId = order.FulfillmentCenterId,
                        fulfillmentCenterName = order.FulfillmentCenterName,
                        currentStock = invPart.CurrentStock,
                        reservedQty = reservedQtyForThisItem,
                        inventoryAvailableQty = inventoryAvailableQty,
                        canUse = canUse,
                        warning = canUse ? null : $"ReservedQty ({reservedQtyForThisItem}) hoặc CurrentStock ({invPart.CurrentStock}) không đủ cho AvailableQty ({oi.AvailableQty})"
                });
            }

            return Ok(new { success = true, data = availableItems });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}