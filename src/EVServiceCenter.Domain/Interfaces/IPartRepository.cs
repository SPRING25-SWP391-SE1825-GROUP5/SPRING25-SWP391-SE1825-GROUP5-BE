using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IPartRepository
    {
        Task<List<Part>> GetAllPartsAsync();
        Task<Part?> GetPartByIdAsync(int partId);
        Task<Part?> GetPartLiteByIdAsync(int partId);
        Task<Part> CreatePartAsync(Part part);
        Task UpdatePartAsync(Part part);
        Task<bool> IsPartNumberUniqueAsync(string partNumber, int? excludePartId = null);
        Task<bool> PartExistsAsync(int partId);
        Task<List<Part>> GetPartsByCategoryIdAsync(int categoryId);
    }
}
