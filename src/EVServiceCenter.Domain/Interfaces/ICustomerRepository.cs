using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetCustomerByUserIdAsync(int userId);
        Task<Customer?> GetCustomerByIdAsync(int customerId);
        Task<Customer?> GetGuestByEmailOrPhoneAsync(string email, string normalizedPhone);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        
    }
}
