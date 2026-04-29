using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface ITenantService
    {
        // Tenant Profile
        Task<ApiResponse> CreateProfileAsync(CreateTenantProfileRequest request, int adminId);
        Task<ApiResponse> UpdateProfileAsync(int tenantId, UpdateTenantProfileRequest request, int adminId);
        Task<ApiResponse> GetProfileByIdAsync(int tenantId);
        Task<ApiResponse> GetAllProfilesAsync(int adminId);
        
        // Tenant Account
        Task<ApiResponse> CreateAccountAsync(CreateTenantAccountRequest request, int adminId);

        // Integrated
        Task<ApiResponse> CreateTenantFullAsync(CreateTenantRequest request, int adminId);
    }
}
