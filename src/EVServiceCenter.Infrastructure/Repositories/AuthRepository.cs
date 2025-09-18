using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly EVDbContext _context;
        public AuthRepository(EVDbContext context) 
        {
            _context = context;        
        }

        public async Task RegisterAsync(User account)
        {
            if (await _context.Users.AnyAsync(u => u.Email == account.Email))
                throw new Exception("Email already exists.");
            _context.Users.Add(account);
            await _context.SaveChangesAsync();
        }

        public async Task<User> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            return user!;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateEmailVerifiedStatusAsync(int userId, bool isVerified)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.EmailVerified = isVerified;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
