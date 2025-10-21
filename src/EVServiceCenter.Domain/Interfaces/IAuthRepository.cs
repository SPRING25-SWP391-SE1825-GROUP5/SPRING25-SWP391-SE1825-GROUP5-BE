using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task RegisterAsync(User account);
        Task<User> LoginAsync(string email, string password);
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<User>> GetAllUsersAsync();
        Task UpdateEmailVerifiedStatusAsync(int userId, bool isVerified);
        Task UpdateUserActiveStatusAsync(int userId, bool isActive);
        Task UpdateUserAsync(User user);
    }
}
