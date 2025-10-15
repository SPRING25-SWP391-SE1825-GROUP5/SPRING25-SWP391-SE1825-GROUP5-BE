using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenancePolicyRepository : IMaintenancePolicyRepository
    {
        private readonly EVDbContext _context;

        public MaintenancePolicyRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<MaintenancePolicy>> GetAllPoliciesAsync()
        {
            return await _context.MaintenancePolicies
                .Include(p => p.Service)
                .OrderByDescending(p => p.PolicyId)
                .ToListAsync();
        }

        public async Task<MaintenancePolicy> GetPolicyByIdAsync(int policyId)
        {
            return await _context.MaintenancePolicies
                .Include(p => p.Service)
                .FirstOrDefaultAsync(p => p.PolicyId == policyId);
        }

        public async Task<List<MaintenancePolicy>> GetActivePoliciesAsync()
        {
            return await _context.MaintenancePolicies
                .Include(p => p.Service)
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.PolicyId)
                .ToListAsync();
        }

        public async Task<List<MaintenancePolicy>> GetPoliciesByServiceIdAsync(int serviceId)
        {
            return await _context.MaintenancePolicies
                .Include(p => p.Service)
                .Where(p => p.ServiceId == serviceId)
                .OrderByDescending(p => p.PolicyId)
                .ToListAsync();
        }

        public async Task<List<MaintenancePolicy>> GetActiveByServiceIdAsync(int serviceId)
        {
            return await _context.MaintenancePolicies
                .Include(p => p.Service)
                .Where(p => p.ServiceId == serviceId && p.IsActive == true)
                .OrderByDescending(p => p.PolicyId)
                .ToListAsync();
        }

        public async Task<MaintenancePolicy> CreatePolicyAsync(MaintenancePolicy policy)
        {
            _context.MaintenancePolicies.Add(policy);
            await _context.SaveChangesAsync();
            return policy;
        }

        public async Task UpdatePolicyAsync(MaintenancePolicy policy)
        {
            _context.MaintenancePolicies.Update(policy);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePolicyAsync(int policyId)
        {
            var policy = await _context.MaintenancePolicies.FindAsync(policyId);
            if (policy != null)
            {
                _context.MaintenancePolicies.Remove(policy);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> PolicyExistsAsync(int policyId)
        {
            return await _context.MaintenancePolicies.AnyAsync(p => p.PolicyId == policyId);
        }
    }
}