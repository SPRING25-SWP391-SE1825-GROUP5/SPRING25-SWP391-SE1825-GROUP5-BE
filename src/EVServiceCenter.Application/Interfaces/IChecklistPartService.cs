using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IChecklistPartService
    {
        Task<AddPartsToChecklistResponse> AddPartsToChecklistAsync(AddPartsToChecklistRequest request);
        Task<RemovePartsFromChecklistResponse> RemovePartsFromChecklistAsync(RemovePartsFromChecklistRequest request);
        Task<List<ServicePartResponse>> GetPartsByServiceIdAsync(int serviceId);
    }
}
