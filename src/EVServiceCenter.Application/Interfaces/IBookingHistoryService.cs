using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IBookingHistoryService
    {
        Task<BookingHistoryListResponse> GetBookingHistoryAsync(int customerId, int page = 1, int pageSize = 10, 
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string sortBy = "bookingDate", string sortOrder = "desc");
        
        Task<BookingHistoryResponse> GetBookingHistoryByIdAsync(int customerId, int bookingId);
        
        Task<BookingHistoryStatsResponse> GetBookingHistoryStatsAsync(int customerId, string period = "all");
    }
}
