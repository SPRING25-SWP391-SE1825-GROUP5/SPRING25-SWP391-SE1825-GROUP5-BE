using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class ServicePackageService : IServicePackageService
{
    private readonly IServicePackageRepository _servicePackageRepository;
    private readonly IServiceRepository _serviceRepository;

    public ServicePackageService(IServicePackageRepository servicePackageRepository, IServiceRepository serviceRepository)
    {
        _servicePackageRepository = servicePackageRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<ServicePackageResponse?> GetByIdAsync(int packageId)
    {
        var servicePackage = await _servicePackageRepository.GetByIdAsync(packageId);
        return servicePackage != null ? MapToResponse(servicePackage) : null;
    }

    public async Task<IEnumerable<ServicePackageResponse>> GetAllAsync()
    {
        var servicePackages = await _servicePackageRepository.GetAllAsync();
        return servicePackages.Select(MapToResponse);
    }

    public async Task<IEnumerable<ServicePackageResponse>> GetActivePackagesAsync()
    {
        var servicePackages = await _servicePackageRepository.GetActivePackagesAsync();
        return servicePackages.Select(MapToResponse);
    }

    public async Task<IEnumerable<ServicePackageResponse>> GetByServiceIdAsync(int serviceId)
    {
        var servicePackages = await _servicePackageRepository.GetByServiceIdAsync(serviceId);
        return servicePackages.Select(MapToResponse);
    }

    public async Task<ServicePackageResponse?> GetByPackageCodeAsync(string packageCode)
    {
        var servicePackage = await _servicePackageRepository.GetByPackageCodeAsync(packageCode);
        return servicePackage != null ? MapToResponse(servicePackage) : null;
    }

    public async Task<ServicePackageResponse> CreateAsync(CreateServicePackageRequest request)
    {
        if (request.ServiceId <= 0)
        {
            throw new ArgumentException("Dịch vụ với ID 0 không tồn tại.");
        }
        // Validate service exists
        if (!await _serviceRepository.ServiceExistsAsync(request.ServiceId))
        {
            throw new ArgumentException($"Dịch vụ với ID {request.ServiceId} không tồn tại.");
        }

        // Validate code unique
        if (await _servicePackageRepository.PackageCodeExistsAsync(request.PackageCode))
        {
            throw new ArgumentException($"Mã gói '{request.PackageCode}' đã tồn tại.");
        }

        // Validate values
        if (request.DiscountPercent.HasValue && (request.DiscountPercent < 0 || request.DiscountPercent > 100))
        {
            throw new ArgumentException("Phần trăm giảm giá phải nằm trong khoảng 0 đến 100.");
        }
        if (request.ValidFrom.HasValue && request.ValidTo.HasValue && request.ValidFrom.Value > request.ValidTo.Value)
        {
            throw new ArgumentException("Thời gian hiệu lực không hợp lệ: 'ValidFrom' phải nhỏ hơn hoặc bằng 'ValidTo'.");
        }

        var servicePackage = new ServicePackage
        {
            PackageName = request.PackageName,
            PackageCode = request.PackageCode,
            Description = request.Description,
            ServiceId = request.ServiceId,
            TotalCredits = request.TotalCredits,
            Price = request.Price,
            DiscountPercent = request.DiscountPercent ?? 0,
            IsActive = request.IsActive,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var createdPackage = await _servicePackageRepository.CreateAsync(servicePackage);
        return MapToResponse(createdPackage);
    }

    public async Task<ServicePackageResponse> UpdateAsync(int packageId, UpdateServicePackageRequest request)
    {
        var servicePackage = await _servicePackageRepository.GetByIdAsync(packageId);
        if (servicePackage == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy gói dịch vụ với ID {packageId}.");
        }

        // Validate service exists if changing service
        if (request.ServiceId.HasValue)
        {
            if (request.ServiceId.Value <= 0)
            {
                throw new ArgumentException("Dịch vụ với ID 0 không tồn tại.");
            }
            if (!await _serviceRepository.ServiceExistsAsync(request.ServiceId.Value))
            {
                throw new ArgumentException($"Dịch vụ với ID {request.ServiceId.Value} không tồn tại.");
            }
        }

        // Validate code unique (excluding current package)
        if (!string.IsNullOrEmpty(request.PackageCode) && await _servicePackageRepository.PackageCodeExistsAsync(request.PackageCode, packageId))
        {
            throw new ArgumentException($"Mã gói '{request.PackageCode}' đã tồn tại.");
        }

        // Validate values (only when provided)
        if (request.DiscountPercent.HasValue && (request.DiscountPercent.Value < 0 || request.DiscountPercent.Value > 100))
        {
            throw new ArgumentException("Phần trăm giảm giá phải nằm trong khoảng 0 đến 100.");
        }
        if (request.ValidFrom.HasValue && request.ValidTo.HasValue && request.ValidFrom.Value > request.ValidTo.Value)
        {
            throw new ArgumentException("Thời gian hiệu lực không hợp lệ: 'ValidFrom' phải nhỏ hơn hoặc bằng 'ValidTo'.");
        }

        // Update properties
        if (!string.IsNullOrEmpty(request.PackageName))
            servicePackage.PackageName = request.PackageName;
        
        if (!string.IsNullOrEmpty(request.PackageCode))
            servicePackage.PackageCode = request.PackageCode;
        
        if (request.Description != null)
            servicePackage.Description = request.Description;
        
        if (request.ServiceId.HasValue)
            servicePackage.ServiceId = request.ServiceId.Value;
        
        if (request.TotalCredits.HasValue)
            servicePackage.TotalCredits = request.TotalCredits.Value;
        
        if (request.Price.HasValue)
            servicePackage.Price = request.Price.Value;
        
        if (request.DiscountPercent.HasValue)
            servicePackage.DiscountPercent = request.DiscountPercent.Value;
        
        if (request.IsActive.HasValue)
            servicePackage.IsActive = request.IsActive.Value;
        
        if (request.ValidFrom.HasValue)
            servicePackage.ValidFrom = request.ValidFrom.Value;
        
        if (request.ValidTo.HasValue)
            servicePackage.ValidTo = request.ValidTo.Value;

        servicePackage.UpdatedAt = DateTime.Now;

        var updatedPackage = await _servicePackageRepository.UpdateAsync(servicePackage);
        return MapToResponse(updatedPackage);
    }

    public async Task DeleteAsync(int packageId)
    {
        if (!await _servicePackageRepository.ExistsAsync(packageId))
        {
            throw new KeyNotFoundException($"Không tìm thấy gói dịch vụ với ID {packageId}.");
        }

        await _servicePackageRepository.DeleteAsync(packageId);
    }

    public async Task<bool> PackageCodeExistsAsync(string packageCode, int? excludeId = null)
    {
        return await _servicePackageRepository.PackageCodeExistsAsync(packageCode, excludeId);
    }

    private static ServicePackageResponse MapToResponse(ServicePackage servicePackage)
    {
        return new ServicePackageResponse
        {
            PackageId = servicePackage.PackageId,
            PackageName = servicePackage.PackageName,
            PackageCode = servicePackage.PackageCode,
            Description = servicePackage.Description,
            ServiceId = servicePackage.ServiceId,
            ServiceName = servicePackage.Service?.ServiceName ?? "N/A",
            TotalCredits = servicePackage.TotalCredits,
            Price = servicePackage.Price,
            DiscountPercent = servicePackage.DiscountPercent,
            IsActive = servicePackage.IsActive,
            ValidFrom = servicePackage.ValidFrom,
            ValidTo = servicePackage.ValidTo,
            CreatedAt = servicePackage.CreatedAt,
            UpdatedAt = servicePackage.UpdatedAt
        };
    }
}
