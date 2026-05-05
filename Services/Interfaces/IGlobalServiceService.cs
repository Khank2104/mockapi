using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IGlobalServiceService
    {
        Task<ApiResponse> GetGlobalServicesAsync();
        Task<ApiResponse> CreateGlobalServiceAsync(ServiceRequest request, int adminId);
        Task<ApiResponse> UpdateGlobalServiceAsync(int serviceId, decimal defaultPrice, int adminId);
        Task<ApiResponse> SeedDefaultServicesAsync(int adminId);
    }
}
