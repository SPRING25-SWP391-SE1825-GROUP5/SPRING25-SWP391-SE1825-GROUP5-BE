using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface ICustomerServiceCreditService
{
    Task<CustomerServiceCreditResponse?> GetByIdAsync(int creditId);
    Task<IEnumerable<CustomerServiceCreditResponse>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<CustomerServiceCreditResponse>> GetActiveCreditsByCustomerIdAsync(int customerId);
    Task<IEnumerable<CustomerServiceCreditResponse>> GetByServiceIdAsync(int serviceId);
    Task<CustomerServiceCreditResponse?> GetActiveCreditForServiceAsync(int customerId, int serviceId);
    Task<CustomerServiceCreditResponse> PurchasePackageAsync(PurchaseServicePackageRequest request);
    Task<CustomerServiceCreditResponse> UseCreditAsync(int creditId, int creditsToUse = 1);
    Task<int> GetRemainingCreditsCountAsync(int customerId, int serviceId);
    Task UpdateExpiredCreditsStatusAsync();
    Task<bool> CanUseCreditAsync(int customerId, int serviceId);
    
    // Service Package APIs for Customer
    Task<IEnumerable<CustomerServicePackageResponse>> GetCustomerServicePackagesAsync(int userId);
    Task<CustomerServicePackageDetailResponse?> GetServicePackageDetailAsync(int packageId, int userId);
    Task<IEnumerable<ServicePackageUsageHistoryResponse>> GetServicePackageUsageHistoryAsync(int packageId, int userId);
    Task<CustomerServicePackageStatisticsResponse> GetCustomerServicePackageStatisticsAsync(int userId);
}
