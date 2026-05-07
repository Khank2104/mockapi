using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface ITenantService
    {
        // Tenant Profile
        Task<ApiResponse> CreateProfileAsync(CreateTenantProfileRequest request, int adminId);
        Task<ApiResponse> UpdateProfileAsync(int tenantId, UpdateTenantProfileRequest request, int adminId);
        Task<ApiResponse> GetProfileByIdAsync(int tenantId);
        Task<ApiResponse> GetAllProfilesAsync(int adminId, string? searchTerm = null, int page = 1, int pageSize = 10);
        
        // Tenant Account
        Task<ApiResponse> CreateAccountAsync(CreateTenantAccountRequest request, int adminId);

        // Integrated
        Task<ApiResponse> CreateTenantFullAsync(CreateTenantRequest request, int adminId);

        // Tenant Self-Service
        Task<ApiResponse> GetProfileByUserIdAsync(int userId);
        Task<ApiResponse> UpdateProfileByUserIdAsync(int userId, UpdateTenantProfileRequest request);
    }
}
