using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IOrderHistoryService
    {
        Task<OrderHistoryListResponse> GetOrderHistoryAsync(int customerId, int page = 1, int pageSize = 10, 
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string sortBy = "orderDate", string sortOrder = "desc");
        
        Task<OrderHistoryResponse> GetOrderHistoryByIdAsync(int customerId, int orderId);
        
        Task<OrderHistoryStatsResponse> GetOrderHistoryStatsAsync(int customerId, string period = "all");
    }
}
