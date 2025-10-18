using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IServicePackageRepository
{
    Task<ServicePackage?> GetByIdAsync(int packageId);
    Task<IEnumerable<ServicePackage>> GetAllAsync();
    Task<IEnumerable<ServicePackage>> GetActivePackagesAsync();
    Task<IEnumerable<ServicePackage>> GetByServiceIdAsync(int serviceId);
    Task<ServicePackage?> GetByPackageCodeAsync(string packageCode);
    Task<ServicePackage> CreateAsync(ServicePackage servicePackage);
    Task<ServicePackage> UpdateAsync(ServicePackage servicePackage);
    Task DeleteAsync(int packageId);
    Task<bool> ExistsAsync(int packageId);
    Task<bool> PackageCodeExistsAsync(string packageCode, int? excludeId = null);
}
