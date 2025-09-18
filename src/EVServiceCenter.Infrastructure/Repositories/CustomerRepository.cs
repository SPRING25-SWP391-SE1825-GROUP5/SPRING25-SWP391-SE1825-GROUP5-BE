using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
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

        public async Task<Customer> GetCustomerByUserIdAsync(int userId)
        {
            return await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Customer> GetCustomerByIdAsync(int customerId)
        {
            return await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
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

        public async Task<bool> IsCustomerCodeUniqueAsync(string customerCode, int? excludeCustomerId = null)
        {
            var query = _context.Customers.Where(c => c.CustomerCode == customerCode);
            
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsPhoneNumberUniqueAsync(string normalizedPhone, int? excludeCustomerId = null)
        {
            var query = _context.Customers.Where(c => c.NormalizedPhone == normalizedPhone);
            
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }

            return !await query.AnyAsync();
        }
    }
}
