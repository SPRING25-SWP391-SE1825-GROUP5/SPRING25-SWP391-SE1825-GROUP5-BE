using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ICustomerRepository _customerRepository;

        public VehicleService(IVehicleRepository vehicleRepository, ICustomerRepository customerRepository)
        {
            _vehicleRepository = vehicleRepository;
            _customerRepository = customerRepository;
        }

        public async Task<VehicleListResponse> GetVehiclesAsync(int pageNumber = 1, int pageSize = 10, int? customerId = null, string? searchTerm = null)
        {
            try
            {
                var vehicles = await _vehicleRepository.GetAllVehiclesAsync();

                if (customerId.HasValue)
                {
                    vehicles = vehicles.Where(v => v.CustomerId == customerId.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    vehicles = vehicles.Where(v =>
                        v.Vin.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.LicensePlate.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Color.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                var totalCount = vehicles.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedVehicles = vehicles.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var vehicleResponses = paginatedVehicles.Select(v => MapToVehicleResponse(v)).ToList();

                return new VehicleListResponse
                {
                    Vehicles = vehicleResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách xe: {ex.Message}");
            }
        }

        public async Task<VehicleResponse> GetVehicleByIdAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                    throw new ArgumentException("Xe không tồn tại.");

                return MapToVehicleResponse(vehicle);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin xe: {ex.Message}");
            }
        }

        public async Task<VehicleResponse> CreateVehicleAsync(CreateVehicleRequest request)
        {
            try
            {
                await ValidateCreateVehicleRequestAsync(request);

                var vehicle = new Vehicle
                {
                    CustomerId = request.CustomerId,
                    ModelId = request.ModelId,
                    Vin = request.Vin.Trim().ToUpper(),
                    LicensePlate = request.LicensePlate.Trim().ToUpper(),
                    Color = request.Color.Trim(),
                    CurrentMileage = request.CurrentMileage,
                    LastServiceDate = request.LastServiceDate,
                    CreatedAt = DateTime.UtcNow
                };

                var createdVehicle = await _vehicleRepository.CreateVehicleAsync(vehicle);

                return MapToVehicleResponse(createdVehicle);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var errorMessage = "Lỗi cơ sở dữ liệu";
                
                if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    switch (sqlEx.Number)
                    {
                        case 547:
                            if (sqlEx.Message.Contains("FK_Vehicles_Models"))
                                errorMessage = "Model xe không tồn tại. Vui lòng chọn model hợp lệ.";
                            else if (sqlEx.Message.Contains("FK_Vehicles_Customers"))
                                errorMessage = "Khách hàng không tồn tại. Vui lòng chọn khách hàng hợp lệ.";
                            else
                                errorMessage = "Dữ liệu tham chiếu không hợp lệ.";
                            break;
                        case 2627:
                            if (sqlEx.Message.Contains("VIN"))
                                errorMessage = "VIN này đã tồn tại. Vui lòng chọn VIN khác.";
                            else if (sqlEx.Message.Contains("LicensePlate"))
                                errorMessage = "Biển số xe này đã tồn tại. Vui lòng chọn biển số khác.";
                            else
                                errorMessage = "Thông tin này đã tồn tại trong hệ thống.";
                            break;
                        default:
                            errorMessage = $"Lỗi cơ sở dữ liệu: {sqlEx.Message}";
                            break;
                    }
                }
                
                throw new ArgumentException(errorMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo xe: {ex.Message}");
            }
        }

        public async Task<VehicleResponse> UpdateVehicleAsync(int vehicleId, UpdateVehicleRequest request)
        {
            try
            {
                var vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                    throw new ArgumentException("Xe không tồn tại.");

                await ValidateUpdateVehicleRequestAsync(request, vehicleId);

                vehicle.Color = request.Color.Trim();
                vehicle.CurrentMileage = request.CurrentMileage;
                vehicle.LastServiceDate = request.LastServiceDate;

                await _vehicleRepository.UpdateVehicleAsync(vehicle);

                return MapToVehicleResponse(vehicle);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật xe: {ex.Message}");
            }
        }


        private VehicleResponse MapToVehicleResponse(Vehicle vehicle)
        {
            return new VehicleResponse
            {
                VehicleId = vehicle.VehicleId,
                CustomerId = vehicle.CustomerId,
                Vin = vehicle.Vin,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                CurrentMileage = vehicle.CurrentMileage,
                LastServiceDate = vehicle.LastServiceDate,
                CreatedAt = vehicle.CreatedAt,
                ModelId = vehicle.ModelId,
                ModelImageUrl = vehicle.VehicleModel?.ImageUrl,
                CustomerName = vehicle.Customer?.User?.FullName ?? "Khách vãng lai",
                CustomerPhone = vehicle.Customer?.User?.PhoneNumber ?? string.Empty
            };
        }

        private async Task ValidateCreateVehicleRequestAsync(CreateVehicleRequest request)
        {
            var errors = new List<string>();

            var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
            var customerDebug = await _customerRepository.GetCustomerByIdDebugAsync(request.CustomerId);
            
            if (customer == null)
            {
                if (customerDebug == null)
                {
                    errors.Add("Khách hàng không tồn tại.");
                }
                else
                {
                    errors.Add("Thông tin người dùng của khách hàng không hợp lệ.");
                }
            }

            if (!await _vehicleRepository.IsVinUniqueAsync(request.Vin.Trim().ToUpper()))
            {
                errors.Add("VIN này đã tồn tại. Vui lòng chọn VIN khác.");
            }

            if (!await _vehicleRepository.IsLicensePlateUniqueAsync(request.LicensePlate.Trim().ToUpper()))
            {
                errors.Add("Biển số xe này đã tồn tại. Vui lòng chọn biển số khác.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private Task ValidateUpdateVehicleRequestAsync(UpdateVehicleRequest request, int vehicleId)
        {
            var errors = new List<string>();

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
            
            return Task.CompletedTask;
        }

        public async Task<CustomerResponse> GetCustomerByVehicleIdAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                    throw new ArgumentException("Xe không tồn tại.");

                var customer = await _customerRepository.GetCustomerByIdAsync(vehicle.CustomerId);
                if (customer == null)
                    throw new ArgumentException("Khách hàng không tồn tại.");

                return new CustomerResponse
                {
                    CustomerId = customer.CustomerId,
                    UserId = customer.UserId,
                    IsGuest = customer.UserId == null,
                    UserFullName = customer.User?.FullName ?? "Khách vãng lai",
                    UserEmail = customer.User?.Email ?? string.Empty,
                    UserPhoneNumber = customer.User?.PhoneNumber ?? string.Empty,
                    VehicleCount = 1
                };
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


        public async Task<VehicleResponse> GetVehicleByVinOrLicensePlateAsync(string vinOrLicensePlate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vinOrLicensePlate))
                    throw new ArgumentException("VIN hoặc biển số xe không được để trống.");

                var vehicles = await _vehicleRepository.GetAllVehiclesAsync();
                var normalizedSearch = vinOrLicensePlate.Trim().ToUpper();

                var vehicle = vehicles.FirstOrDefault(v => 
                    v.Vin.Equals(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    v.LicensePlate.Equals(normalizedSearch, StringComparison.OrdinalIgnoreCase));

                if (vehicle == null)
                    throw new ArgumentException("Không tìm thấy xe với VIN hoặc biển số này.");

                return MapToVehicleResponse(vehicle);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tìm xe: {ex.Message}");
            }
        }
    }
}
