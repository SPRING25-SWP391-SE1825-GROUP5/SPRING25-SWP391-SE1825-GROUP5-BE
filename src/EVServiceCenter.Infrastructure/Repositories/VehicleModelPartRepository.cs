using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class VehicleModelPartRepository : IVehicleModelPartRepository
    {
        private readonly EVDbContext _context;

        public VehicleModelPartRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<VehicleModelPart?> GetByIdAsync(int id)
        {
            return await _context.VehicleModelParts
                .Include(vmp => vmp.VehicleModel)
                .Include(vmp => vmp.Part)
                .FirstOrDefaultAsync(vmp => vmp.Id == id);
        }

        public async Task<IEnumerable<VehicleModelPart>> GetByModelIdAsync(int modelId)
        {
            return await _context.VehicleModelParts
                .Include(vmp => vmp.VehicleModel)
                .Include(vmp => vmp.Part)
                .Where(vmp => vmp.ModelId == modelId)
                .OrderBy(vmp => vmp.Part.PartName)
                .ToListAsync();
        }

        public async Task<IEnumerable<VehicleModelPart>> GetByPartIdAsync(int partId)
        {
            return await _context.VehicleModelParts
                .Include(vmp => vmp.VehicleModel)
                .Include(vmp => vmp.Part)
                .Where(vmp => vmp.PartId == partId)
                .OrderBy(vmp => vmp.VehicleModel.Brand)
                .ThenBy(vmp => vmp.VehicleModel.ModelName)
                .ToListAsync();
        }

        public async Task<VehicleModelPart?> GetByModelAndPartIdAsync(int modelId, int partId)
        {
            return await _context.VehicleModelParts
                .FirstOrDefaultAsync(vmp => vmp.ModelId == modelId && vmp.PartId == partId);
        }

        public async Task<IEnumerable<VehicleModelPart>> GetCompatiblePartsByModelIdAsync(int modelId)
        {
            return await _context.VehicleModelParts
                .Include(vmp => vmp.VehicleModel)
                .Include(vmp => vmp.Part)
                .Where(vmp => vmp.ModelId == modelId && vmp.IsCompatible)
                .OrderBy(vmp => vmp.Part.PartName)
                .ToListAsync();
        }

        public async Task<IEnumerable<VehicleModelPart>> GetIncompatiblePartsByModelIdAsync(int modelId)
        {
            return await _context.VehicleModelParts
                .Include(vmp => vmp.VehicleModel)
                .Include(vmp => vmp.Part)
                .Where(vmp => vmp.ModelId == modelId && !vmp.IsCompatible)
                .OrderBy(vmp => vmp.Part.PartName)
                .ToListAsync();
        }

        public async Task<VehicleModelPart> CreateAsync(VehicleModelPart modelPart)
        {
            _context.VehicleModelParts.Add(modelPart);
            await _context.SaveChangesAsync();
            return modelPart;
        }

        public async Task<VehicleModelPart> UpdateAsync(VehicleModelPart modelPart)
        {
            _context.VehicleModelParts.Update(modelPart);
            await _context.SaveChangesAsync();
            return modelPart;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var modelPart = await _context.VehicleModelParts.FindAsync(id);
            if (modelPart == null)
                return false;

            _context.VehicleModelParts.Remove(modelPart);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountCompatiblePartsByModelIdAsync(int modelId)
        {
            return await _context.VehicleModelParts
                .CountAsync(vmp => vmp.ModelId == modelId && vmp.IsCompatible);
        }

        public async Task<int> CountIncompatiblePartsByModelIdAsync(int modelId)
        {
            return await _context.VehicleModelParts
                .CountAsync(vmp => vmp.ModelId == modelId && !vmp.IsCompatible);
        }
    }
}
