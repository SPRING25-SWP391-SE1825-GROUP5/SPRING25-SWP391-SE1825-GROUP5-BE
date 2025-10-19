using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class ServicePackageRepository : IServicePackageRepository
{
    private readonly EVDbContext _context;

    public ServicePackageRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<ServicePackage?> GetByIdAsync(int packageId)
    {
        return await _context.ServicePackages
            .Include(sp => sp.Service)
            .FirstOrDefaultAsync(sp => sp.PackageId == packageId);
    }

    public async Task<IEnumerable<ServicePackage>> GetAllAsync()
    {
        return await _context.ServicePackages
            .Include(sp => sp.Service)
            .OrderBy(sp => sp.PackageName)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServicePackage>> GetActivePackagesAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.ServicePackages
            .Include(sp => sp.Service)
            .Where(sp => sp.IsActive && 
                        (sp.ValidFrom == null || sp.ValidFrom <= now) &&
                        (sp.ValidTo == null || sp.ValidTo >= now))
            .OrderBy(sp => sp.PackageName)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServicePackage>> GetByServiceIdAsync(int serviceId)
    {
        return await _context.ServicePackages
            .Include(sp => sp.Service)
            .Where(sp => sp.ServiceId == serviceId && sp.IsActive)
            .OrderBy(sp => sp.PackageName)
            .ToListAsync();
    }

    public async Task<ServicePackage?> GetByPackageCodeAsync(string packageCode)
    {
        return await _context.ServicePackages
            .Include(sp => sp.Service)
            .FirstOrDefaultAsync(sp => sp.PackageCode == packageCode);
    }

    public async Task<ServicePackage> CreateAsync(ServicePackage servicePackage)
    {
        _context.ServicePackages.Add(servicePackage);
        await _context.SaveChangesAsync();
        // Ensure navigation loaded so API can return ServiceName
        await _context.Entry(servicePackage).Reference(sp => sp.Service).LoadAsync();
        return servicePackage;
    }

    public async Task<ServicePackage> UpdateAsync(ServicePackage servicePackage)
    {
        // Bypass EF OUTPUT clause due to DB trigger by using raw SQL UPDATE
        var sql = @"UPDATE [dbo].[ServicePackages]
SET [PackageName] = {0},
    [PackageCode] = {1},
    [Description] = {2},
    [ServiceId] = {3},
    [TotalCredits] = {4},
    [Price] = {5},
    [DiscountPercent] = {6},
    [IsActive] = {7},
    [ValidFrom] = {8},
    [ValidTo] = {9},
    [UpdatedAt] = {10}
WHERE [PackageId] = {11};";

        var parameters = new object[]
        {
            servicePackage.PackageName ?? string.Empty,
            servicePackage.PackageCode ?? string.Empty,
            (object?)servicePackage.Description ?? DBNull.Value,
            servicePackage.ServiceId,
            servicePackage.TotalCredits,
            servicePackage.Price,
            servicePackage.DiscountPercent ?? 0m,
            servicePackage.IsActive,
            (object?)servicePackage.ValidFrom ?? DBNull.Value,
            (object?)servicePackage.ValidTo ?? DBNull.Value,
            DateTime.UtcNow,
            servicePackage.PackageId
        };

        var rows = await _context.Database.ExecuteSqlRawAsync(sql, parameters);

        // Reload entity from DB with navigation
        var reloaded = await _context.ServicePackages
            .Include(sp => sp.Service)
            .FirstAsync(sp => sp.PackageId == servicePackage.PackageId);
        return reloaded;
    }

    public async Task DeleteAsync(int packageId)
    {
        var servicePackage = await _context.ServicePackages.FindAsync(packageId);
        if (servicePackage != null)
        {
            _context.ServicePackages.Remove(servicePackage);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int packageId)
    {
        return await _context.ServicePackages.AnyAsync(sp => sp.PackageId == packageId);
    }

    public async Task<bool> PackageCodeExistsAsync(string packageCode, int? excludeId = null)
    {
        var query = _context.ServicePackages.Where(sp => sp.PackageCode == packageCode);
        
        if (excludeId.HasValue)
        {
            query = query.Where(sp => sp.PackageId != excludeId.Value);
        }
        
        return await query.AnyAsync();
    }
}
