using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface ICustomerServiceCreditRepository
{
    Task<CustomerServiceCredit?> GetByIdAsync(int creditId);
    Task<IEnumerable<CustomerServiceCredit>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<CustomerServiceCredit>> GetActiveCreditsByCustomerIdAsync(int customerId);
    Task<IEnumerable<CustomerServiceCredit>> GetByServiceIdAsync(int serviceId);
    Task<IEnumerable<CustomerServiceCredit>> GetExpiredCreditsAsync();
    Task<CustomerServiceCredit?> GetActiveCreditForServiceAsync(int customerId, int serviceId);
    Task<IEnumerable<CustomerServiceCredit>> GetByCustomerAndPackageAsync(int customerId, int packageId);
    Task<CustomerServiceCredit> CreateAsync(CustomerServiceCredit customerServiceCredit);
    Task<CustomerServiceCredit> UpdateAsync(CustomerServiceCredit customerServiceCredit);
    Task DeleteAsync(int creditId);
    Task<bool> ExistsAsync(int creditId);
    Task<int> GetRemainingCreditsCountAsync(int customerId, int serviceId);
    Task UpdateExpiredCreditsStatusAsync();
    Task<bool> CanUseCreditAsync(int customerId, int serviceId);
}
