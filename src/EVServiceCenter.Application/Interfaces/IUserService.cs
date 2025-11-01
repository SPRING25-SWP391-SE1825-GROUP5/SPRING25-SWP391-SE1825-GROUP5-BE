using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserListResponse> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? role = null);
        Task<UserResponse> GetUserByIdAsync(int userId);
        Task<UserResponse> CreateUserAsync(CreateUserRequest request);
        Task<bool> ActivateUserAsync(int userId);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
        Task<bool> AssignUserRoleAsync(int userId, string role);
        Task<int> GetUsersCountAsync(string? searchTerm = null, string? role = null, bool? isActive = null, bool? emailVerified = null, DateTime? createdFrom = null, DateTime? createdTo = null);
        Task<IList<UserResponse>> GetUsersForExportAsync(string? searchTerm = null, string? role = null, int maxRecords = 100000, bool? isActive = null, bool? emailVerified = null, DateTime? createdFrom = null, DateTime? createdTo = null);
    }
}
