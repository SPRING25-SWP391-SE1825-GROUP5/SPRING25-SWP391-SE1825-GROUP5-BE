using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IInventoryReportsService
    {
        Task<InventoryUsageResponse> GetInventoryUsageAsync(int centerId, string period = "month");
    }
}
