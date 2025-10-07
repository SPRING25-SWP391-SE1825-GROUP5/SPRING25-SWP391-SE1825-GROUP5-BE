using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IWorkOrderService
    {
        Task<WorkOrder> AssignTechnicianAsync(int workOrderId, int technicianId);
        Task<List<WorkOrder>> GetByTechnicianAsync(int technicianId, DateTime? from, DateTime? to, string status);
    }
}

 
