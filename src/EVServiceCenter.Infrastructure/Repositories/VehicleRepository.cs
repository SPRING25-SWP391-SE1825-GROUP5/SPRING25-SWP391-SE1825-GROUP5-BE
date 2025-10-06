using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly EVDbContext _context;

        public VehicleRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetAllVehiclesAsync()
        {
            return await _context.Vehicles
                .Include(v => v.Customer)
                .Include(v => v.Customer.User)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<Vehicle> GetVehicleByIdAsync(int vehicleId)
        {
            return await _context.Vehicles
                .Include(v => v.Customer)
                .Include(v => v.Customer.User)
                .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);
        }

        public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task UpdateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleAsync(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsVinUniqueAsync(string vin, int? excludeVehicleId = null)
        {
            var query = _context.Vehicles.Where(v => v.Vin == vin);
            
            if (excludeVehicleId.HasValue)
            {
                query = query.Where(v => v.VehicleId != excludeVehicleId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsLicensePlateUniqueAsync(string licensePlate, int? excludeVehicleId = null)
        {
            var query = _context.Vehicles.Where(v => v.LicensePlate == licensePlate);
            
            if (excludeVehicleId.HasValue)
            {
                query = query.Where(v => v.VehicleId != excludeVehicleId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> VehicleExistsAsync(int vehicleId)
        {
            return await _context.Vehicles.AnyAsync(v => v.VehicleId == vehicleId);
        }

        public async Task<int> CountByModelIdAsync(int modelId)
        {
            return await _context.Vehicles
                .CountAsync(v => v.ModelId == modelId);
        }
    }
}
