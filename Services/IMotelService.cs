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
        
        // Room
        Task<ApiResponse> CreateRoomAsync(RoomRequest request, int adminId);
        Task<ApiResponse> UpdateRoomAsync(int roomId, RoomRequest request, int adminId);
        
        // Settings
        Task<ApiResponse> UpdateRoomSettingAsync(RoomSettingRequest request, int adminId);
        Task<ApiResponse> UpdateRoomServiceSettingAsync(RoomServiceSettingRequest request, int adminId);
        Task<ApiResponse> GetRoomSettingsAsync(int roomId, int adminId);
        Task<ApiResponse> GetRoomServicesAsync(int roomId, int adminId);
        Task<ApiResponse> GetRoomOccupantsAsync(int roomId, int adminId);
    }
}
