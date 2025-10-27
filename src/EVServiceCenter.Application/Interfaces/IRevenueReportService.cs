using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IRevenueReportService
    {
        Task<RevenueReportResponse> GetRevenueReportAsync(int centerId, RevenueReportRequest request);
    }
}
