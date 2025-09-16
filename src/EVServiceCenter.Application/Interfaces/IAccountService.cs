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
        Task<User> GetAccountByPhoneNumberAsync(string email);
        Task<User> GetAccountByEmailAsync(string phoneNumber);
        Task<User> CreateAccountAsync(User account);
    }
}
