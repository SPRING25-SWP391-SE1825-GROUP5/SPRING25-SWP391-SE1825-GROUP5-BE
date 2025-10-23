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
        Task<User?> GetAccountByPhoneNumberAsync(string phoneNumber);
        Task<User?> GetAccountByEmailAsync(string email);
        Task<User> CreateAccountAsync(User account);
        Task UpdateAccountAsync(User user);
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<User>> GetAllUsersWithRoleAsync(string role);
    }
}
