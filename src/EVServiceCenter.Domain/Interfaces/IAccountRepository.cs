using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.IRepositories
{
    public interface IAccountRepository
    {
        Task<User?> GetAccountByPhoneNumberAsync(string email);
        Task<User?> GetAccountByEmailAsync(string phoneNumber);
        Task<User> CreateAccountAsync(User account);
        Task UpdateAccountAsync(User user);
    }
}
