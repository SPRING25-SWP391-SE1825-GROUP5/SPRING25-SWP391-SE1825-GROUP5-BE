using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerResponse> GetCurrentCustomerAsync(int userId);
        Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
        Task<CustomerResponse> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request);
        Task<CustomerResponse> QuickCreateCustomerAsync(QuickCreateCustomerRequest request);
    }
}
