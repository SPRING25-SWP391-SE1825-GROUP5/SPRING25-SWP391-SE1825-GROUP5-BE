using System.Threading.Tasks;
using EVServiceCenter.Application.Models;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IBookingStatisticsService
    {
        Task<BookingStatisticsResponse> GetBookingStatisticsAsync(BookingStatisticsRequest request);
        Task<BookingStatisticsResponse> GetCenterBookingStatisticsAsync(CenterBookingStatisticsRequest request);
    }
}
