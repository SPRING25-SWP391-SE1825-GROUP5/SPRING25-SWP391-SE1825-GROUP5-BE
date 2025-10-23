using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly EVDbContext _context;
    
        public AccountRepository(EVDbContext context) 
        {
            _context = context;
        }

        public async Task<User?> GetAccountByPhoneNumberAsync(string phoneNumber)
        {
            var account = await _context.Users.FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            return account;
        }

        public async Task<User?> GetAccountByEmailAsync(string email)
        {
            var account = await _context.Users.FirstOrDefaultAsync(a => a.Email == email);
            return account;
        }
        public async Task<User> CreateAccountAsync(User account)
        {
            await _context.Users.AddAsync(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task UpdateAccountAsync(User user) 
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<List<User>> GetAllUsersWithRoleAsync(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .ToListAsync();
        }
    }
}
