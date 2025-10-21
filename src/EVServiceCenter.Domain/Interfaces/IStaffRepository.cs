using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IStaffRepository
    {
        Task<List<Staff>> GetAllStaffAsync();
        Task<Staff?> GetStaffByIdAsync(int staffId);
        Task<Staff?> GetStaffByUserIdAsync(int userId);
        Task<List<Staff>> GetStaffByCenterIdAsync(int centerId);
        Task<Staff> CreateStaffAsync(Staff staff);
        Task UpdateStaffAsync(Staff staff);
        Task DeleteStaffAsync(int staffId);
        Task<bool> StaffExistsAsync(int staffId);
        Task<bool> IsUserAlreadyStaffAsync(int userId);
        Task<bool> ExistsActiveByUserAsync(int userId);
    }
}
