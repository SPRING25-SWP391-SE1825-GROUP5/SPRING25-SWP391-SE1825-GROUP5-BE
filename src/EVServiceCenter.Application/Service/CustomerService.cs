using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;

namespace EVServiceCenter.Application.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IBookingRepository _bookingRepository;
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerServiceCreditRepository _customerServiceCreditRepository;
        private readonly IPromotionRepository _promotionRepository;

        public CustomerService(ICustomerRepository customerRepository, IAccountRepository accountRepository, IEmailService emailService, IBookingRepository bookingRepository, IWorkOrderPartRepository workOrderPartRepository, IInvoiceRepository invoiceRepository, ICustomerServiceCreditRepository customerServiceCreditRepository, IPromotionRepository promotionRepository)
        {
            _customerRepository = customerRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _bookingRepository = bookingRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _invoiceRepository = invoiceRepository;
            _customerServiceCreditRepository = customerServiceCreditRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<List<User>> GetAllUsersWithCustomerRoleAsync()
        {
            return await _accountRepository.GetAllUsersWithRoleAsync("CUSTOMER");
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _customerRepository.GetAllCustomersAsync();
        }

        public async Task<CustomerResponse> GetCurrentCustomerAsync(int userId)
        {
            try
            {
                var user = await _accountRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("Người dùng không tồn tại.");
                }

                if (user.Role != "CUSTOMER")
                {
                    throw new ArgumentException($"Tài khoản với vai trò '{user.Role}' không thể có thông tin khách hàng. Chỉ tài khoản CUSTOMER mới có thể truy cập thông tin này.");
                }

                var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
                if (customer == null)
                {
                    var newCustomer = new Customer
                    {
                        UserId = userId,
                        IsGuest = false
                    };

                    customer = await _customerRepository.CreateCustomerAsync(newCustomer);
                }

                return MapToCustomerResponse(customer);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khách hàng: {ex.Message}");
            }
        }

        public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreateCustomerRequestAsync(request);

                // Normalize phone number
                var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

                // Create customer entity
                var customer = new Customer
                {
                    UserId = null, // Will be set when user registers
                    IsGuest = request.IsGuest,

                };

                // Save customer
                var createdCustomer = await _customerRepository.CreateCustomerAsync(customer);

                return MapToCustomerResponse(createdCustomer);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo khách hàng: {ex.Message}");
            }
        }

        public async Task<CustomerResponse> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
        {
            try
            {
                // Validate customer exists
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    throw new ArgumentException("Khách hàng không tồn tại.");

                // Validate request
                await ValidateUpdateCustomerRequestAsync(request, customerId);

                // Normalize phone number
                var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

                // Update customer
                customer.IsGuest = request.IsGuest;


                await _customerRepository.UpdateCustomerAsync(customer);

                return MapToCustomerResponse(customer);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật khách hàng: {ex.Message}");
            }
        }

        public async Task<CustomerResponse> QuickCreateCustomerAsync(QuickCreateCustomerRequest request)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("Họ tên không được trống");
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email không được trống");
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new ArgumentException("Số điện thoại không được trống");

            var email = request.Email.Trim().ToLowerInvariant();
            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

            // Unique checks
            var existingByEmail = await _accountRepository.GetAccountByEmailAsync(email);
            if (existingByEmail != null)
                throw new ArgumentException("Email đã tồn tại");

            var existingByPhone = await _accountRepository.GetAccountByPhoneNumberAsync(normalizedPhone);
            if (existingByPhone != null)
                throw new ArgumentException("Số điện thoại đã tồn tại");

            // Generate secure random password (>=8, upper/lower/digit/special)
            var tempPassword = GenerateSecurePassword(12);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            // Create user
            var user = new User
            {
                Email = email,
                FullName = request.FullName.Trim(),
                PhoneNumber = normalizedPhone,
                PasswordHash = passwordHash,
                Role = "CUSTOMER",
                IsActive = true,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _accountRepository.CreateAccountAsync(user);

            // Create customer
            var customer = new Customer
            {
                UserId = user.UserId,
                IsGuest = false,
            };

            var created = await _customerRepository.CreateCustomerAsync(customer);

            // Gửi email chào mừng kèm mật khẩu tạm
            await _emailService.SendWelcomeCustomerWithPasswordAsync(user.Email, user.FullName, tempPassword);

            return MapToCustomerResponse(created);
        }

        private static string GenerateSecurePassword(int length)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "!@#$%^&*()-_=+";

            var rng = RandomNumberGenerator.Create();
            string Pick(string chars)
            {
                var b = new byte[1];
                rng.GetBytes(b);
                return chars[b[0] % chars.Length].ToString();
            }

            var sb = new StringBuilder();
            sb.Append(Pick(upper));
            sb.Append(Pick(lower));
            sb.Append(Pick(digits));
            sb.Append(Pick(specials));

            var all = upper + lower + digits + specials;
            while (sb.Length < Math.Max(8, length))
            {
                sb.Append(Pick(all));
            }

            return sb.ToString();
        }

        private CustomerResponse MapToCustomerResponse(Customer customer)
        {
            return new CustomerResponse
            {
                CustomerId = customer.CustomerId,
                UserId = customer.UserId,
                IsGuest = customer.IsGuest,
                UserFullName = customer.User?.FullName ?? string.Empty,
                UserEmail = customer.User?.Email ?? string.Empty,
                UserPhoneNumber = customer.User?.PhoneNumber ?? string.Empty,
                VehicleCount = customer.Vehicles?.Count ?? 0
            };
        }

        private Task ValidateCreateCustomerRequestAsync(CreateCustomerRequest request)
        {
            var errors = new List<string>();

            // CustomerCode/NormalizedPhone removed from Customer; uniqueness handled on User

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));

            return Task.CompletedTask;
        }

        private Task ValidateUpdateCustomerRequestAsync(UpdateCustomerRequest request, int customerId)
        {
            var errors = new List<string>();

            // CustomerCode/NormalizedPhone removed from Customer; uniqueness handled on User

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));

            return Task.CompletedTask;
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters and ensure it starts with 0
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return digits.StartsWith("0") ? digits : "0" + digits;
        }

        public async Task<CustomerBookingsResponse> GetCustomerBookingsAsync(int customerId, int pageNumber = 1, int pageSize = 10)
        {
            var response = new CustomerBookingsResponse
            {
                CustomerId = customerId,
                Bookings = new List<CustomerBookingItem>()
            };

            try
            {
                // Optimize: Use paginated query instead of loading all bookings
                var bookings = await _bookingRepository.GetBookingsByCustomerIdAsync(
                    customerId, pageNumber, pageSize, null, null, null, "createdAt", "desc");

                // Get total count for pagination
                var totalItems = await _bookingRepository.CountBookingsByCustomerIdAsync(customerId, null, null, null);
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                // Optimize: Batch load all workOrderParts, invoices, and promotions in parallel
                var bookingIds = bookings.Select(b => b.BookingId).ToList();

                // Batch load workOrderParts for all bookings
                var allWorkOrderParts = new List<Domain.Entities.WorkOrderPart>();
                foreach (var bookingId in bookingIds)
                {
                    var parts = await _workOrderPartRepository.GetByBookingIdAsync(bookingId);
                    allWorkOrderParts.AddRange(parts);
                }
                var workOrderPartsByBooking = allWorkOrderParts.GroupBy(p => p.BookingId).ToDictionary(g => g.Key, g => g.ToList());

                // Batch load invoices for all bookings
                var invoicesByBooking = new Dictionary<int, Domain.Entities.Invoice>();
                foreach (var bookingId in bookingIds)
                {
                    var invoice = await _invoiceRepository.GetByBookingIdAsync(bookingId);
                    if (invoice != null)
                    {
                        invoicesByBooking[bookingId] = invoice;
                    }
                }

                // Batch load appliedCredits for bookings that need them
                var appliedCreditIds = bookings.Where(b => b.AppliedCreditId.HasValue).Select(b => b.AppliedCreditId!.Value).Distinct().ToList();
                var appliedCreditsById = new Dictionary<int, Domain.Entities.CustomerServiceCredit>();
                foreach (var creditId in appliedCreditIds)
                {
                    var credit = await _customerServiceCreditRepository.GetByIdAsync(creditId);
                    if (credit != null)
                    {
                        appliedCreditsById[creditId] = credit;
                    }
                }

                // Batch load promotions for all bookings
                var promotionsByBooking = new Dictionary<int, List<Domain.Entities.UserPromotion>>();
                foreach (var bookingId in bookingIds)
                {
                    var promotions = await _promotionRepository.GetUserPromotionsByBookingAsync(bookingId);
                    if (promotions != null && promotions.Any())
                    {
                        promotionsByBooking[bookingId] = promotions;
                    }
                }

                // Process each booking using pre-loaded data
                foreach (var booking in bookings)
                {
                    // Tính giá dịch vụ
                    var servicePrice = booking.Service?.BasePrice ?? 0m;

                    // Tính giá phụ tùng phát sinh (chỉ tính phụ tùng đã CONSUMED và không phải khách cung cấp)
                    var workOrderParts = workOrderPartsByBooking.GetValueOrDefault(booking.BookingId, new List<Domain.Entities.WorkOrderPart>());
                    var partsAmount = workOrderParts
                        .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
                        .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0m));

                    // Lấy package discount và promotion discount từ Invoice nếu có (chính xác hơn)
                    // Nếu chưa có invoice, tính từ booking data
                    decimal packageDiscountAmount = 0m;
                    decimal promotionDiscountAmount = 0m;

                    if (invoicesByBooking.TryGetValue(booking.BookingId, out var invoice))
                    {
                        // Có invoice: lấy từ invoice (chính xác nhất)
                        packageDiscountAmount = invoice.PackageDiscountAmount;
                        promotionDiscountAmount = invoice.PromotionDiscountAmount;
                    }
                    else
                    {
                        // Chưa có invoice: tính từ booking data
                        // Tính package discount nếu có AppliedCredit
                        if (booking.AppliedCreditId.HasValue && appliedCreditsById.TryGetValue(booking.AppliedCreditId.Value, out var appliedCredit))
                        {
                            if (appliedCredit?.ServicePackage != null)
                            {
                                packageDiscountAmount = servicePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
                            }
                        }

                        // Tính promotion discount từ UserPromotions
                        if (promotionsByBooking.TryGetValue(booking.BookingId, out var userPromotions))
                        {
                            promotionDiscountAmount = userPromotions
                                .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                                .Sum(up => up.DiscountAmount);
                        }
                    }

                    // Tính giá dịch vụ sau khi trừ package discount
                    var finalServicePrice = servicePrice - packageDiscountAmount;

                    // Khuyến mãi chỉ áp dụng cho phần dịch vụ, không áp dụng cho parts
                    if (promotionDiscountAmount > finalServicePrice)
                    {
                        promotionDiscountAmount = finalServicePrice;
                    }

                    // Tính tổng: FinalServicePrice + PartsAmount - PromotionDiscountAmount
                    var totalAmount = finalServicePrice + partsAmount - promotionDiscountAmount;

                    response.Bookings.Add(new CustomerBookingItem
                    {
                        BookingId = booking.BookingId,
                        Status = booking.Status ?? "N/A",
                        Date = booking.TechnicianTimeSlot?.WorkDate.ToString("yyyy-MM-dd") ?? "N/A",
                        SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "N/A",
                        SlotLabel = booking.TechnicianTimeSlot?.Slot?.SlotLabel != "SA" && booking.TechnicianTimeSlot?.Slot?.SlotLabel != "CH" ? booking.TechnicianTimeSlot?.Slot?.SlotLabel : null,
                        ServiceName = booking.Service?.ServiceName ?? "N/A",
                        CenterName = booking.Center?.CenterName ?? "N/A",
                        VehiclePlate = booking.Vehicle?.LicensePlate ?? "N/A",
                        SpecialRequests = booking.SpecialRequests ?? "N/A",
                        CreatedAt = booking.CreatedAt,
                        ActualCost = totalAmount > 0 ? totalAmount : null,
                        EstimatedCost = servicePrice > 0 ? servicePrice : null,
                        BookingCode = $"BK{booking.BookingId:D6}", // Generate booking code from BookingId
                        TechnicianName = booking.TechnicianTimeSlot?.Technician?.User?.FullName
                    });
                }

                // Add pagination info
                response.Pagination = new EVServiceCenter.Application.Models.PaginationInfo
                {
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalItems,
                    TotalPages = totalPages
                };

                return response;
            }
            catch (Exception)
            {
                return new CustomerBookingsResponse
                {
                    CustomerId = customerId,
                    Bookings = new List<CustomerBookingItem>()
                };
            }
        }

        public async Task<int?> GetCustomerUserIdAsync(int customerId)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                return customer?.UserId;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

