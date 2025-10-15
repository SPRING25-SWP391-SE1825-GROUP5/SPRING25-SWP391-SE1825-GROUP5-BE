using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/service-maintenance")]
    public class ServiceMaintenanceController : ControllerBase
    {
        private readonly IMaintenancePolicyService _policyService;
        private readonly IMaintenanceChecklistItemService _checklistItemService;
        private readonly IServiceService _serviceService;

        public ServiceMaintenanceController(
            IMaintenancePolicyService policyService,
            IMaintenanceChecklistItemService checklistItemService,
            IServiceService serviceService)
        {
            _policyService = policyService;
            _checklistItemService = checklistItemService;
            _serviceService = serviceService;
        }

        /// <summary>
        /// Thêm chính sách bảo trì vào dịch vụ
        /// </summary>
        [HttpPost("services/{serviceId:int}/maintenance-policies")]
        public async Task<IActionResult> AddMaintenancePoliciesToService(
            int serviceId, 
            [FromBody] AddMaintenancePoliciesToServiceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                if (request.ServiceId != serviceId)
                {
                    return BadRequest(new { success = false, message = "ServiceId trong URL và body không khớp" });
                }

                // Validate service exists
                var service = await _serviceService.GetServiceByIdAsync(serviceId);
                if (service == null)
                {
                    return NotFound(new { success = false, message = "Dịch vụ không tồn tại" });
                }

                var addedPolicies = new List<MaintenancePolicyResponse>();

                // Create each policy
                foreach (var policyData in request.Policies)
                {
                    var createRequest = new CreateMaintenancePolicyRequest
                    {
                        ServiceId = serviceId,
                        IntervalMonths = policyData.IntervalMonths,
                        IntervalKm = policyData.IntervalKm,
                        IsActive = policyData.IsActive
                    };

                    var createdPolicy = await _policyService.CreatePolicyAsync(createRequest);
                    addedPolicies.Add(createdPolicy);
                }

                var response = new AddMaintenancePoliciesToServiceResponse
                {
                    ServiceId = serviceId,
                    ServiceName = service.ServiceName,
                    AddedPoliciesCount = addedPolicies.Count,
                    AddedPolicies = addedPolicies
                };

                return Ok(new { success = true, data = response });
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

        /// <summary>
        /// Thêm mục kiểm tra bảo trì vào dịch vụ
        /// </summary>
        [HttpPost("services/{serviceId:int}/maintenance-checklist")]
        public async Task<IActionResult> AddMaintenanceChecklistToService(
            int serviceId, 
            [FromBody] AddMaintenanceChecklistToServiceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                if (request.ServiceId != serviceId)
                {
                    return BadRequest(new { success = false, message = "ServiceId trong URL và body không khớp" });
                }

                // Validate service exists
                var service = await _serviceService.GetServiceByIdAsync(serviceId);
                if (service == null)
                {
                    return NotFound(new { success = false, message = "Dịch vụ không tồn tại" });
                }

                var addedItems = new List<MaintenanceChecklistItemResponse>();

                // Create each checklist item
                foreach (var itemData in request.Items)
                {
                    var createRequest = new CreateMaintenanceChecklistItemRequest
                    {
                        ItemName = itemData.ItemName,
                        Description = itemData.Description
                    };

                    var createdItem = await _checklistItemService.CreateItemAsync(createRequest);
                    addedItems.Add(createdItem);
                }

                var response = new AddMaintenanceChecklistToServiceResponse
                {
                    ServiceId = serviceId,
                    ServiceName = service.ServiceName,
                    AddedItemsCount = addedItems.Count,
                    AddedItems = addedItems
                };

                return Ok(new { success = true, data = response });
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
    }
}



