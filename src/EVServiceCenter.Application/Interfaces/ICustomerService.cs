using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerResponse> GetCurrentCustomerAsync(int userId);
        Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
        Task<CustomerResponse> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request);
        Task<CustomerResponse> QuickCreateCustomerAsync(QuickCreateCustomerRequest request);
        Task<List<User>> GetAllUsersWithCustomerRoleAsync();
        Task<List<Customer>> GetAllCustomersAsync();
    }
}
