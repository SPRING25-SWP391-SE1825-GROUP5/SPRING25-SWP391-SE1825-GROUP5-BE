using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class VehicleModelRepository : IVehicleModelRepository
    {
        private readonly EVDbContext _context;

        public VehicleModelRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<VehicleModel?> GetByIdAsync(int id)
        {
            return await _context.VehicleModels
                .FirstOrDefaultAsync(vm => vm.ModelId == id);
        }

        public async Task<IEnumerable<VehicleModel>> GetAllAsync()
        {
            return await _context.VehicleModels
                .OrderByDescending(vm => vm.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<VehicleModel>> GetByBrandAsync(string brand)
        {
            return await _context.VehicleModels
                .Where(vm => vm.Brand.ToLower().Contains(brand.ToLower()))
                .OrderByDescending(vm => vm.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<VehicleModel>> GetActiveModelsAsync()
        {
            return await _context.VehicleModels
                .Where(vm => vm.IsActive)
                .OrderByDescending(vm => vm.CreatedAt)
                .ToListAsync();
        }

        public async Task<VehicleModel> CreateAsync(VehicleModel model)
        {
            _context.VehicleModels.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<VehicleModel> UpdateAsync(VehicleModel model)
        {
            _context.VehicleModels.Update(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var model = await _context.VehicleModels.FindAsync(id);
            if (model == null)
                return false;

            _context.VehicleModels.Remove(model);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<VehicleModel>> SearchAsync(string searchTerm)
        {
            return await _context.VehicleModels
                .Where(vm => vm.ModelName.ToLower().Contains(searchTerm.ToLower()) ||
                           vm.Brand.ToLower().Contains(searchTerm.ToLower()))
                .OrderByDescending(vm => vm.CreatedAt)
                .ToListAsync();
        }
    }
}
