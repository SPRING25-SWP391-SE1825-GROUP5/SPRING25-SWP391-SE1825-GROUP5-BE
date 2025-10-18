using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenanceChecklistRepository
    {
        Task<MaintenanceChecklist?> GetByBookingIdAsync(int bookingId);
        Task<MaintenanceChecklist> CreateAsync(MaintenanceChecklist checklist);
    }
}


