using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IServiceBookingStatsService
    {
        Task<ServiceBookingStatsResponse> GetServiceBookingStatsAsync(ServiceBookingStatsRequest? request = null);
    }
}


