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
        var creditsList = credits.ToList();

        if (!creditsList.Any())
        {
            return Enumerable.Empty<CustomerServiceCreditResponse>();
        }

        var packageIds = creditsList.Select(c => c.PackageId).Distinct().ToList();
        var serviceIds = creditsList.Select(c => c.ServiceId).Distinct().ToList();

        var packageDict = new Dictionary<int, ServicePackage>();
        foreach (var packageId in packageIds)
        {
            var package = await _servicePackageRepository.GetByIdAsync(packageId);
            if (package != null)
            {
                packageDict[packageId] = package;
            }
        }

        var serviceDict = new Dictionary<int, EVServiceCenter.Domain.Entities.Service>();
        foreach (var serviceId in serviceIds)
        {
            var service = await _serviceRepository.GetServiceByIdAsync(serviceId);
            if (service != null)
            {
                serviceDict[serviceId] = service;
            }
        }

        var result = new List<CustomerServiceCreditResponse>();
        foreach (var credit in creditsList)
        {
            var package = packageDict.ContainsKey(credit.PackageId) ? packageDict[credit.PackageId] : null;
            var service = serviceDict.ContainsKey(credit.ServiceId) ? serviceDict[credit.ServiceId] : null;

            result.Add(new CustomerServiceCreditResponse
            {
                CreditId = credit.CreditId,
                CustomerId = credit.CustomerId,
                CustomerName = "N/A",
                PackageId = credit.PackageId,
                PackageName = package?.PackageName ?? "N/A",
                PackageCode = package?.PackageCode ?? "N/A",
                ServiceId = credit.ServiceId,
                ServiceName = service?.ServiceName ?? "N/A",
                TotalCredits = credit.TotalCredits,
                UsedCredits = credit.UsedCredits,
                RemainingCredits = credit.TotalCredits - credit.UsedCredits,
                PurchaseDate = credit.PurchaseDate,
                ExpiryDate = credit.ExpiryDate,
                Status = credit.Status,
                CreatedAt = credit.CreatedAt,
                UpdatedAt = credit.UpdatedAt
            });
        }

        return result;
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
        var remainingCredits = credit.TotalCredits - credit.UsedCredits;
        
        var customerName = "N/A";
        try
        {
            customerName = credit.Customer?.User?.FullName ?? "N/A";
        }
        catch
        {
            customerName = "N/A";
        }
        
        return new CustomerServiceCreditResponse
        {
            CreditId = credit.CreditId,
            CustomerId = credit.CustomerId,
            CustomerName = customerName,
            PackageId = credit.PackageId,
            PackageName = credit.ServicePackage?.PackageName ?? "N/A",
            PackageCode = credit.ServicePackage?.PackageCode ?? "N/A",
            ServiceId = credit.ServiceId,
            ServiceName = credit.Service?.ServiceName ?? "N/A",
            TotalCredits = credit.TotalCredits,
            UsedCredits = credit.UsedCredits,
            RemainingCredits = remainingCredits,
            PurchaseDate = credit.PurchaseDate,
            ExpiryDate = credit.ExpiryDate,
            Status = credit.Status,
            CreatedAt = credit.CreatedAt,
            UpdatedAt = credit.UpdatedAt
        };
    }

    // Service Package APIs for Customer
    public async Task<IEnumerable<CustomerServicePackageResponse>> GetCustomerServicePackagesAsync(int userId)
    {
        var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
        if (customer == null) throw new ArgumentException("Không tìm thấy khách hàng");

        var credits = await _customerServiceCreditRepository.GetByCustomerIdAsync(customer.CustomerId);
        return credits.Select(MapToServicePackageResponse);
    }

    public async Task<CustomerServicePackageDetailResponse?> GetServicePackageDetailAsync(int packageId, int userId)
    {
        var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
        if (customer == null) throw new ArgumentException("Không tìm thấy khách hàng");

        var credit = await _customerServiceCreditRepository.GetByCustomerIdAndPackageIdAsync(customer.CustomerId, packageId);
        if (credit == null) return null;

        return MapToServicePackageDetailResponse(credit);
    }

    public async Task<IEnumerable<ServicePackageUsageHistoryResponse>> GetServicePackageUsageHistoryAsync(int packageId, int userId)
    {
        var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
        if (customer == null) throw new ArgumentException("Không tìm thấy khách hàng");

        var credit = await _customerServiceCreditRepository.GetByCustomerIdAndPackageIdAsync(customer.CustomerId, packageId);
        if (credit == null) return new List<ServicePackageUsageHistoryResponse>();

        // Get bookings that used this credit
        var bookings = await _customerServiceCreditRepository.GetBookingsByCreditIdAsync(credit.CreditId);
        return bookings.Select(MapToUsageHistoryResponse);
    }

    public async Task<CustomerServicePackageStatisticsResponse> GetCustomerServicePackageStatisticsAsync(int userId)
    {
        var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
        if (customer == null) throw new ArgumentException("Không tìm thấy khách hàng");

        var credits = await _customerServiceCreditRepository.GetByCustomerIdAsync(customer.CustomerId);
        
        return new CustomerServicePackageStatisticsResponse
        {
            TotalPackages = credits.Count(),
            ActivePackages = credits.Count(c => c.Status == "ACTIVE"),
            ExpiredPackages = credits.Count(c => c.Status == "EXPIRED"),
            UsedUpPackages = credits.Count(c => c.Status == "USED_UP"),
            TotalCreditsPurchased = credits.Sum(c => c.TotalCredits),
            TotalCreditsUsed = credits.Sum(c => c.UsedCredits),
            TotalCreditsRemaining = credits.Sum(c => c.RemainingCredits),
            TotalAmountSpent = credits.Sum(c => c.ServicePackage?.Price ?? 0),
            TotalSavings = credits.Sum(c => (c.ServicePackage?.Price ?? 0) * (c.ServicePackage?.DiscountPercent ?? 0) / 100),
            FirstPurchaseDate = credits.Min(c => c.CreatedAt),
            LastPurchaseDate = credits.Max(c => c.CreatedAt),
            LastUsageDate = credits.Max(c => c.UpdatedAt),
            TopServices = GetTopServices(credits),
            MonthlyStats = GetMonthlyStats(credits)
        };
    }

    private CustomerServicePackageResponse MapToServicePackageResponse(CustomerServiceCredit credit)
    {
        return new CustomerServicePackageResponse
        {
            CreditId = credit.CreditId,
            PackageId = credit.PackageId,
            PackageName = credit.ServicePackage?.PackageName ?? "",
            PackageDescription = credit.ServicePackage?.Description ?? "",
            OriginalPrice = credit.ServicePackage?.Price ?? 0,
            DiscountPercent = credit.ServicePackage?.DiscountPercent ?? 0,
            FinalPrice = (credit.ServicePackage?.Price ?? 0) * (1 - (credit.ServicePackage?.DiscountPercent ?? 0) / 100),
            TotalCredits = credit.TotalCredits,
            UsedCredits = credit.UsedCredits,
            RemainingCredits = credit.RemainingCredits,
            Status = credit.Status,
            PurchaseDate = credit.CreatedAt,
            ExpiryDate = credit.ExpiryDate,
            ServiceName = credit.ServicePackage?.Service?.ServiceName ?? "",
            ServiceDescription = credit.ServicePackage?.Service?.Description ?? ""
        };
    }

    private CustomerServicePackageDetailResponse MapToServicePackageDetailResponse(CustomerServiceCredit credit)
    {
        return new CustomerServicePackageDetailResponse
        {
            CreditId = credit.CreditId,
            PackageId = credit.PackageId,
            PackageName = credit.ServicePackage?.PackageName ?? "",
            PackageDescription = credit.ServicePackage?.Description ?? "",
            OriginalPrice = credit.ServicePackage?.Price ?? 0,
            DiscountPercent = credit.ServicePackage?.DiscountPercent ?? 0,
            FinalPrice = (credit.ServicePackage?.Price ?? 0) * (1 - (credit.ServicePackage?.DiscountPercent ?? 0) / 100),
            TotalCredits = credit.TotalCredits,
            UsedCredits = credit.UsedCredits,
            RemainingCredits = credit.RemainingCredits,
            Status = credit.Status,
            PurchaseDate = credit.CreatedAt,
            ExpiryDate = credit.ExpiryDate,
            ServiceInfo = new ServicePackageServiceInfo
            {
                ServiceId = credit.ServicePackage?.ServiceId ?? 0,
                ServiceName = credit.ServicePackage?.Service?.ServiceName ?? "",
                ServiceDescription = credit.ServicePackage?.Service?.Description ?? "",
                BasePrice = credit.ServicePackage?.Service?.BasePrice ?? 0,
                EstimatedDuration = 0 // Service entity không có EstimatedDuration
            },
            UsageSummary = new ServicePackageUsageSummary
            {
                TotalBookings = 0, // TODO: Implement booking count
                CompletedBookings = 0, // TODO: Implement completed count
                CancelledBookings = 0, // TODO: Implement cancelled count
                TotalSavings = (credit.ServicePackage?.Price ?? 0) * (credit.ServicePackage?.DiscountPercent ?? 0) / 100 * credit.UsedCredits,
                LastUsedDate = credit.UpdatedAt
            }
        };
    }

    private ServicePackageUsageHistoryResponse MapToUsageHistoryResponse(Booking booking)
    {
        return new ServicePackageUsageHistoryResponse
        {
            BookingId = booking.BookingId,
            BookingCode = $"BK{booking.BookingId:D6}",
            BookingDate = booking.CreatedAt,
            Status = booking.Status ?? "",
            OriginalPrice = booking.Service?.BasePrice ?? 0,
            DiscountAmount = 0, // TODO: Calculate discount amount
            FinalPrice = 0, // TODO: Calculate final price
            CreditsUsed = 1,
            ServiceName = booking.Service?.ServiceName ?? "",
            CenterName = booking.Center?.CenterName ?? "",
            VehicleLicensePlate = booking.LicensePlate ?? "",
            CreatedAt = booking.CreatedAt
        };
    }

    private List<ServiceUsageStatistic> GetTopServices(IEnumerable<CustomerServiceCredit> credits)
    {
        return credits
            .Where(c => c.ServicePackage?.Service != null)
            .GroupBy(c => c.ServicePackage!.Service!.ServiceId)
            .Select(g => new ServiceUsageStatistic
            {
                ServiceId = g.Key,
                ServiceName = g.First().ServicePackage!.Service!.ServiceName,
                UsageCount = g.Sum(c => c.UsedCredits),
                TotalSavings = g.Sum(c => ((c.ServicePackage?.Price ?? 0) * (c.ServicePackage?.DiscountPercent ?? 0) / 100) * c.UsedCredits)
            })
            .OrderByDescending(s => s.UsageCount)
            .Take(5)
            .ToList();
    }

    private List<MonthlyStatistic> GetMonthlyStats(IEnumerable<CustomerServiceCredit> credits)
    {
        return credits
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new MonthlyStatistic
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                PackagesPurchased = g.Count(),
                CreditsUsed = g.Sum(c => c.UsedCredits),
                AmountSpent = g.Sum(c => c.ServicePackage?.Price ?? 0),
                Savings = g.Sum(c => (c.ServicePackage?.Price ?? 0) * (c.ServicePackage?.DiscountPercent ?? 0) / 100)
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .Take(12)
            .ToList();
    }
}
