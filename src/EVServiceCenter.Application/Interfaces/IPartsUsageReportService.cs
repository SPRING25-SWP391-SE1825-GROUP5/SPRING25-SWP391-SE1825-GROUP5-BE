using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPartsUsageReportService
    {
        Task<PartsUsageReportResponse> GetPartsUsageReportAsync(int centerId, PartsUsageReportRequest request);
    }
}
