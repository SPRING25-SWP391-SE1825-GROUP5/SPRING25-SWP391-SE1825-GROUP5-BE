using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly EVDbContext _context;

        public StaffRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Staff>> GetAllStaffAsync()
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Center)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Center)
                .FirstOrDefaultAsync(s => s.StaffId == staffId);
        }

        public async Task<Staff?> GetStaffByUserIdAsync(int userId)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Center)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<List<Staff>> GetStaffByCenterIdAsync(int? centerId)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Center)
                .Where(s => centerId.HasValue ? s.CenterId == centerId.Value : s.CenterId == 0)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Staff> CreateStaffAsync(Staff staff)
        {
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();
            return staff;
        }

        public async Task UpdateStaffAsync(Staff staff)
        {
            _context.Staff.Update(staff);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStaffAsync(int staffId)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff != null)
            {
                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> StaffExistsAsync(int staffId)
        {
            return await _context.Staff.AnyAsync(s => s.StaffId == staffId);
        }

        public async Task<bool> IsUserAlreadyStaffAsync(int userId)
        {
            return await _context.Staff.AnyAsync(s => s.UserId == userId);
        }

        public async Task<bool> ExistsActiveByUserAsync(int userId)
        {
            return await _context.Staff.AnyAsync(s => s.UserId == userId && s.IsActive);
        }
    }
}
