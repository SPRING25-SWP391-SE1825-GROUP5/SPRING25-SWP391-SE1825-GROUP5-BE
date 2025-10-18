using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly EVDbContext _context;

        public CustomerRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetCustomerByUserIdAsync(int userId)
        {
            return await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            return await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<Customer?> GetGuestByEmailOrPhoneAsync(string email, string normalizedPhone)
        {
            var query = _context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .Where(c => c.IsGuest);

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(c => c.User != null && c.User.Email == email);
            }
            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                query = query.Where(c => c.User != null && c.User.PhoneNumber == normalizedPhone);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CustomerExistsAsync(int customerId)
        {
            return await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
        }

        // CustomerCode & NormalizedPhone removed from Customer; uniqueness now handled on Users
    }
}
