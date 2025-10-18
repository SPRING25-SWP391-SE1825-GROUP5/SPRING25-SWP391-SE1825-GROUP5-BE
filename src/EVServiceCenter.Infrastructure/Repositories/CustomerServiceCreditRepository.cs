using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class CustomerServiceCreditRepository : ICustomerServiceCreditRepository
{
    private readonly EVDbContext _context;

    public CustomerServiceCreditRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerServiceCredit?> GetByIdAsync(int creditId)
    {
        return await _context.CustomerServiceCredits
            .Include(csc => csc.Customer)
            .Include(csc => csc.ServicePackage)
            .Include(csc => csc.Service)
            .FirstOrDefaultAsync(csc => csc.CreditId == creditId);
    }

    public async Task<IEnumerable<CustomerServiceCredit>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.CustomerServiceCredits
            .Include(csc => csc.ServicePackage)
            .Include(csc => csc.Service)
            .Where(csc => csc.CustomerId == customerId)
            .OrderByDescending(csc => csc.PurchaseDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerServiceCredit>> GetActiveCreditsByCustomerIdAsync(int customerId)
    {
        var now = DateTime.UtcNow;
        return await _context.CustomerServiceCredits
            .Include(csc => csc.ServicePackage)
            .Include(csc => csc.Service)
            .Where(csc => csc.CustomerId == customerId && 
                         csc.Status == "ACTIVE" &&
                         csc.RemainingCredits > 0 &&
                         (csc.ExpiryDate == null || csc.ExpiryDate >= now))
            .OrderByDescending(csc => csc.PurchaseDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerServiceCredit>> GetByServiceIdAsync(int serviceId)
    {
        return await _context.CustomerServiceCredits
            .Include(csc => csc.Customer)
            .Include(csc => csc.ServicePackage)
            .Where(csc => csc.ServiceId == serviceId)
            .OrderByDescending(csc => csc.PurchaseDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerServiceCredit>> GetExpiredCreditsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.CustomerServiceCredits
            .Where(csc => csc.Status == "ACTIVE" && 
                         csc.ExpiryDate != null && 
                         csc.ExpiryDate < now)
            .ToListAsync();
    }

    public async Task<CustomerServiceCredit?> GetActiveCreditForServiceAsync(int customerId, int serviceId)
    {
        var now = DateTime.UtcNow;
        return await _context.CustomerServiceCredits
            .Include(csc => csc.ServicePackage)
            .Include(csc => csc.Service)
            .Where(csc => csc.CustomerId == customerId && 
                         csc.ServiceId == serviceId &&
                         csc.Status == "ACTIVE" &&
                         csc.RemainingCredits > 0 &&
                         (csc.ExpiryDate == null || csc.ExpiryDate >= now))
            .OrderByDescending(csc => csc.PurchaseDate)
            .FirstOrDefaultAsync();
    }

    public async Task<CustomerServiceCredit> CreateAsync(CustomerServiceCredit customerServiceCredit)
    {
        _context.CustomerServiceCredits.Add(customerServiceCredit);
        await _context.SaveChangesAsync();
        return customerServiceCredit;
    }

    public async Task<CustomerServiceCredit> UpdateAsync(CustomerServiceCredit customerServiceCredit)
    {
        customerServiceCredit.UpdatedAt = DateTime.UtcNow;
        _context.CustomerServiceCredits.Update(customerServiceCredit);
        await _context.SaveChangesAsync();
        return customerServiceCredit;
    }

    public async Task DeleteAsync(int creditId)
    {
        var customerServiceCredit = await _context.CustomerServiceCredits.FindAsync(creditId);
        if (customerServiceCredit != null)
        {
            _context.CustomerServiceCredits.Remove(customerServiceCredit);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int creditId)
    {
        return await _context.CustomerServiceCredits.AnyAsync(csc => csc.CreditId == creditId);
    }

    public async Task<int> GetRemainingCreditsCountAsync(int customerId, int serviceId)
    {
        var now = DateTime.UtcNow;
        return await _context.CustomerServiceCredits
            .Where(csc => csc.CustomerId == customerId && 
                         csc.ServiceId == serviceId &&
                         csc.Status == "ACTIVE" &&
                         (csc.ExpiryDate == null || csc.ExpiryDate >= now))
            .SumAsync(csc => csc.RemainingCredits);
    }

    public async Task UpdateExpiredCreditsStatusAsync()
    {
        var now = DateTime.UtcNow;
        var expiredCredits = await _context.CustomerServiceCredits
            .Where(csc => csc.Status == "ACTIVE" && 
                         csc.ExpiryDate != null && 
                         csc.ExpiryDate < now)
            .ToListAsync();

        foreach (var credit in expiredCredits)
        {
            credit.Status = "EXPIRED";
            credit.UpdatedAt = now;
        }

        if (expiredCredits.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CanUseCreditAsync(int customerId, int serviceId)
    {
        var now = DateTime.UtcNow;
        return await _context.CustomerServiceCredits
            .AnyAsync(csc => csc.CustomerId == customerId && 
                           csc.ServiceId == serviceId &&
                           csc.Status == "ACTIVE" &&
                           csc.RemainingCredits > 0 &&
                           (csc.ExpiryDate == null || csc.ExpiryDate >= now));
    }
}
