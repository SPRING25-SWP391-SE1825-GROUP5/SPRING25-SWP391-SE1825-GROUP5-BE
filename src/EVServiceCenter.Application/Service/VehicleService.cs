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

        public async Task<VehicleListResponse> GetVehiclesAsync(int pageNumber = 1, int pageSize = 10, int? customerId = null, string searchTerm = null)
        {
            try
            {
                var vehicles = await _vehicleRepository.GetAllVehiclesAsync();

                // Filtering
                if (customerId.HasValue)
                {
                    vehicles = vehicles.Where(v => v.CustomerId == customerId.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    vehicles = vehicles.Where(v =>
                        v.Vin.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.LicensePlate.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Color.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Model.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Model.ModelName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Pagination
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
                throw; // Rethrow validation errors
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
                // Validate request
                await ValidateCreateVehicleRequestAsync(request);

                // Create vehicle entity
                var vehicle = new Vehicle
                {
                    CustomerId = request.CustomerId,
                    ModelId = request.ModelId,
                    Vin = request.Vin.Trim().ToUpper(),
                    LicensePlate = request.LicensePlate.Trim().ToUpper(),
                    Color = request.Color.Trim(),
                    CurrentMileage = request.CurrentMileage,
                    LastServiceDate = request.LastServiceDate,
                    NextServiceDue = request.NextServiceDue,
                    CreatedAt = DateTime.UtcNow
                };

                // Save vehicle
                var createdVehicle = await _vehicleRepository.CreateVehicleAsync(vehicle);

                return MapToVehicleResponse(createdVehicle);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
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
                // Validate vehicle exists
                var vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                    throw new ArgumentException("Xe không tồn tại.");

                // Validate request
                await ValidateUpdateVehicleRequestAsync(request, vehicleId);

                // Update vehicle
                vehicle.ModelId = request.ModelId;
                vehicle.Vin = request.Vin.Trim().ToUpper();
                vehicle.LicensePlate = request.LicensePlate.Trim().ToUpper();
                vehicle.Color = request.Color.Trim();
                vehicle.CurrentMileage = request.CurrentMileage;
                vehicle.LastServiceDate = request.LastServiceDate;
                vehicle.NextServiceDue = request.NextServiceDue;

                await _vehicleRepository.UpdateVehicleAsync(vehicle);

                return MapToVehicleResponse(vehicle);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật xe: {ex.Message}");
            }
        }

        public async Task<bool> DeleteVehicleAsync(int vehicleId)
        {
            try
            {
                // Validate vehicle exists
                if (!await _vehicleRepository.VehicleExistsAsync(vehicleId))
                    throw new ArgumentException("Xe không tồn tại.");

                await _vehicleRepository.DeleteVehicleAsync(vehicleId);
                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa xe: {ex.Message}");
            }
        }

        private VehicleResponse MapToVehicleResponse(Vehicle vehicle)
        {
            return new VehicleResponse
            {
                VehicleId = vehicle.VehicleId,
                CustomerId = vehicle.CustomerId,
                ModelId = vehicle.ModelId,
                Vin = vehicle.Vin,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                CurrentMileage = vehicle.CurrentMileage,
                LastServiceDate = vehicle.LastServiceDate,
                NextServiceDue = vehicle.NextServiceDue,
                CreatedAt = vehicle.CreatedAt,
                CustomerName = vehicle.Customer?.User?.FullName ?? "Khách vãng lai",
                CustomerPhone = vehicle.Customer?.User?.PhoneNumber ?? vehicle.Customer?.NormalizedPhone,
                ModelBrand = vehicle.Model?.Brand,
                ModelName = vehicle.Model?.ModelName,
                ModelYear = vehicle.Model?.Year ?? 0,
                BatteryCapacity = vehicle.Model?.BatteryCapacity,
                Range = vehicle.Model?.Range
            };
        }

        private async Task ValidateCreateVehicleRequestAsync(CreateVehicleRequest request)
        {
            var errors = new List<string>();

            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
            if (customer == null)
            {
                errors.Add("Khách hàng không tồn tại.");
            }

            // Check for duplicate VIN
            if (!await _vehicleRepository.IsVinUniqueAsync(request.Vin.Trim().ToUpper()))
            {
                errors.Add("VIN này đã tồn tại. Vui lòng chọn VIN khác.");
            }

            // Check for duplicate license plate
            if (!await _vehicleRepository.IsLicensePlateUniqueAsync(request.LicensePlate.Trim().ToUpper()))
            {
                errors.Add("Biển số xe này đã tồn tại. Vui lòng chọn biển số khác.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateUpdateVehicleRequestAsync(UpdateVehicleRequest request, int vehicleId)
        {
            var errors = new List<string>();

            // Check for duplicate VIN
            if (!await _vehicleRepository.IsVinUniqueAsync(request.Vin.Trim().ToUpper(), vehicleId))
            {
                errors.Add("VIN này đã tồn tại. Vui lòng chọn VIN khác.");
            }

            // Check for duplicate license plate
            if (!await _vehicleRepository.IsLicensePlateUniqueAsync(request.LicensePlate.Trim().ToUpper(), vehicleId))
            {
                errors.Add("Biển số xe này đã tồn tại. Vui lòng chọn biển số khác.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }
    }
}
