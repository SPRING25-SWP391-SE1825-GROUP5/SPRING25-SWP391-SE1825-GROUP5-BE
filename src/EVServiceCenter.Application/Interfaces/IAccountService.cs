using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IAccountService
    {
        Task<User?> GetAccountByPhoneNumberAsync(string phoneNumber);
        Task<User?> GetAccountByEmailAsync(string email);
        Task<User> CreateAccountAsync(User account);
    }
}
