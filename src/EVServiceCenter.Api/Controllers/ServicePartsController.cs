using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/services/{serviceId:int}/parts")]
    public class ServicePartsController : ControllerBase
    {
        private readonly IServicePartRepository _servicePartRepo;
        public ServicePartsController(IServicePartRepository servicePartRepo)
        {
            _servicePartRepo = servicePartRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int serviceId)
        {
            var items = await _servicePartRepo.GetByServiceIdAsync(serviceId);
            var result = items.Select(x => new
            {
                partId = x.PartId,
                partName = x.Part?.PartName,
                notes = x.Notes
            });
            return Ok(new { success = true, data = result });
        }

        public class ServicePartsReplaceRequest
        {
            public List<Item> Parts { get; set; }
            public class Item
            {
                public int PartId { get; set; }
                public string Notes { get; set; }
            }
        }

        [HttpPut]
        public async Task<IActionResult> Replace(int serviceId, [FromBody] ServicePartsReplaceRequest request)
        {
            var toSave = (request.Parts ?? new List<ServicePartsReplaceRequest.Item>())
                .DistinctBy(p => p.PartId)
                .Select(p => new ServicePart { ServiceId = serviceId, PartId = p.PartId, Notes = p.Notes });

            await _servicePartRepo.ReplaceForServiceAsync(serviceId, toSave);
            return Ok(new { success = true });
        }

        public class ServicePartAddRequest { public int PartId { get; set; } public string Notes { get; set; } }

        [HttpPost]
        public async Task<IActionResult> Add(int serviceId, [FromBody] ServicePartAddRequest request)
        {
            await _servicePartRepo.AddAsync(new ServicePart { ServiceId = serviceId, PartId = request.PartId, Notes = request.Notes });
            return Ok(new { success = true });
        }

        [HttpDelete("{partId:int}")]
        public async Task<IActionResult> Delete(int serviceId, int partId)
        {
            await _servicePartRepo.DeleteAsync(serviceId, partId);
            return Ok(new { success = true });
        }
    }
}


