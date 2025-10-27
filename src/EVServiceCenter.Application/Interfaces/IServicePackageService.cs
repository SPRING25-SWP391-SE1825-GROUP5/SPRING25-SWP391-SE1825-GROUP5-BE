using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IServicePackageService
{
    Task<ServicePackageResponse?> GetByIdAsync(int packageId);
    Task<IEnumerable<ServicePackageResponse>> GetAllAsync();
    Task<IEnumerable<ServicePackageResponse>> GetActivePackagesAsync();
    Task<IEnumerable<ServicePackageResponse>> GetByServiceIdAsync(int serviceId);
    Task<ServicePackageResponse?> GetByPackageCodeAsync(string packageCode);
    Task<ServicePackageResponse> CreateAsync(CreateServicePackageRequest request);
    Task<ServicePackageResponse> UpdateAsync(int packageId, UpdateServicePackageRequest request);
    Task DeleteAsync(int packageId);
    Task<bool> PackageCodeExistsAsync(string packageCode, int? excludeId = null);
}
