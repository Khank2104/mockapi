using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IMotelService
    {
        // Motel
        Task<ApiResponse> CreateMotelAsync(MotelRequest request, int adminId);
        Task<ApiResponse> UpdateMotelAsync(int motelId, MotelRequest request, int adminId);
        Task<ApiResponse> GetMotelsByAdminAsync(int adminId);
        
        // Floor
        Task<ApiResponse> CreateFloorAsync(FloorRequest request, int adminId);
        Task<ApiResponse> UpdateFloorAsync(int floorId, FloorRequest request, int adminId);
        
    }
}
