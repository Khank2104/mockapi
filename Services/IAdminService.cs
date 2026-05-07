using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IAdminService
    {
        Task<ApiResponse> CreateAdminAsync(CreateAdminRequest request, int superuserId);
        Task<ApiResponse> GetAllAdminsAsync(int superuserId);
        Task<ApiResponse> GetAdminByIdAsync(int adminId, int superuserId);
        Task<ApiResponse> ToggleAdminStatusAsync(int adminId, bool active, int superuserId);
        Task<ApiResponse> UpdateAdminAsync(int adminId, UpdateAdminRequest request, int superuserId);
        Task<ApiResponse> DeleteAdminAsync(int adminId, int superuserId);
    }
}
