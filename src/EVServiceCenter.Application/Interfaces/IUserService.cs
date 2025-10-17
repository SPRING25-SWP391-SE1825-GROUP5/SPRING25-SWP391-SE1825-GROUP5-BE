using System.Threading.Tasks;
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
        Task<bool> AssignUserRoleAsync(int userId, string role);
    }
}
