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

public class CustomerServiceCreditService : ICustomerServiceCreditService
{
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepository;
    private readonly IServicePackageRepository _servicePackageRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IServiceRepository _serviceRepository;

    public CustomerServiceCreditService(
        ICustomerServiceCreditRepository customerServiceCreditRepository,
        IServicePackageRepository servicePackageRepository,
        ICustomerRepository customerRepository,
        IServiceRepository serviceRepository)
    {
        _customerServiceCreditRepository = customerServiceCreditRepository;
        _servicePackageRepository = servicePackageRepository;
        _customerRepository = customerRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<CustomerServiceCreditResponse?> GetByIdAsync(int creditId)
    {
        var credit = await _customerServiceCreditRepository.GetByIdAsync(creditId);
        return credit != null ? MapToResponse(credit) : null;
    }

    public async Task<IEnumerable<CustomerServiceCreditResponse>> GetByCustomerIdAsync(int customerId)
    {
        var credits = await _customerServiceCreditRepository.GetByCustomerIdAsync(customerId);
        return credits.Select(MapToResponse);
    }

    public async Task<IEnumerable<CustomerServiceCreditResponse>> GetActiveCreditsByCustomerIdAsync(int customerId)
    {
        var credits = await _customerServiceCreditRepository.GetActiveCreditsByCustomerIdAsync(customerId);
        return credits.Select(MapToResponse);
    }

    public async Task<IEnumerable<CustomerServiceCreditResponse>> GetByServiceIdAsync(int serviceId)
    {
        var credits = await _customerServiceCreditRepository.GetByServiceIdAsync(serviceId);
        return credits.Select(MapToResponse);
    }

    public async Task<CustomerServiceCreditResponse?> GetActiveCreditForServiceAsync(int customerId, int serviceId)
    {
        var credit = await _customerServiceCreditRepository.GetActiveCreditForServiceAsync(customerId, serviceId);
        return credit != null ? MapToResponse(credit) : null;
    }

    public async Task<CustomerServiceCreditResponse> PurchasePackageAsync(PurchaseServicePackageRequest request)
    {
        // Validate customer exists
        if (!await _customerRepository.CustomerExistsAsync(request.CustomerId))
        {
            throw new ArgumentException($"Customer with ID {request.CustomerId} not found.");
        }

        // Validate service package exists and is active
        var servicePackage = await _servicePackageRepository.GetByIdAsync(request.PackageId);
        if (servicePackage == null || !servicePackage.IsActive)
        {
            throw new ArgumentException($"Service package with ID {request.PackageId} not found or inactive.");
        }

        // Validate service exists
        int serviceId = request.ServiceId ?? servicePackage.ServiceId;
        if (!await _serviceRepository.ServiceExistsAsync(serviceId))
        {
            throw new ArgumentException($"Service with ID {serviceId} does not exist.");
        }

        var customerServiceCredit = new CustomerServiceCredit
        {
            CustomerId = request.CustomerId,
            PackageId = request.PackageId,
            ServiceId = serviceId,
            TotalCredits = servicePackage.TotalCredits,
            UsedCredits = 0,
            PurchaseDate = DateTime.Now,
            ExpiryDate = request.ExpiryDate ?? servicePackage.ValidTo,
            Status = "ACTIVE",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var createdCredit = await _customerServiceCreditRepository.CreateAsync(customerServiceCredit);
        return MapToResponse(createdCredit);
    }

    public async Task<CustomerServiceCreditResponse> UseCreditAsync(int creditId, int creditsToUse = 1)
    {
        var credit = await _customerServiceCreditRepository.GetByIdAsync(creditId);
        if (credit == null)
        {
            throw new KeyNotFoundException($"Customer service credit with ID {creditId} not found.");
        }

        if (credit.Status != "ACTIVE")
        {
            throw new InvalidOperationException("Cannot use an inactive credit.");
        }

        if (credit.ExpiryDate.HasValue && credit.ExpiryDate < DateTime.Now)
        {
            throw new InvalidOperationException("Cannot use an expired credit.");
        }

        if (credit.RemainingCredits < creditsToUse)
        {
            throw new InvalidOperationException($"Not enough credits available. Remaining: {credit.RemainingCredits}, Requested: {creditsToUse}.");
        }

        credit.UsedCredits += creditsToUse;
        credit.UpdatedAt = DateTime.Now;

        if (credit.RemainingCredits == 0)
        {
            credit.Status = "USED_UP";
        }

        var updatedCredit = await _customerServiceCreditRepository.UpdateAsync(credit);
        return MapToResponse(updatedCredit);
    }

    public async Task<int> GetRemainingCreditsCountAsync(int customerId, int serviceId)
    {
        return await _customerServiceCreditRepository.GetRemainingCreditsCountAsync(customerId, serviceId);
    }

    public async Task UpdateExpiredCreditsStatusAsync()
    {
        await _customerServiceCreditRepository.UpdateExpiredCreditsStatusAsync();
    }

    public async Task<bool> CanUseCreditAsync(int customerId, int serviceId)
    {
        return await _customerServiceCreditRepository.CanUseCreditAsync(customerId, serviceId);
    }

    private static CustomerServiceCreditResponse MapToResponse(CustomerServiceCredit credit)
    {
        return new CustomerServiceCreditResponse
        {
            CreditId = credit.CreditId,
            CustomerId = credit.CustomerId,
            CustomerName = credit.Customer?.User?.FullName ?? "N/A",
            PackageId = credit.PackageId,
            PackageName = credit.ServicePackage?.PackageName ?? "N/A",
            PackageCode = credit.ServicePackage?.PackageCode ?? "N/A",
            ServiceId = credit.ServiceId,
            ServiceName = credit.Service?.ServiceName ?? "N/A",
            TotalCredits = credit.TotalCredits,
            UsedCredits = credit.UsedCredits,
            RemainingCredits = credit.RemainingCredits,
            PurchaseDate = credit.PurchaseDate,
            ExpiryDate = credit.ExpiryDate,
            Status = credit.Status,
            CreatedAt = credit.CreatedAt,
            UpdatedAt = credit.UpdatedAt
        };
    }
}
