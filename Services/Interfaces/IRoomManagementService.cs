using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IRoomManagementService
    {
        Task<ApiResponse> CreateRoomAsync(RoomRequest request, int adminId);
        Task<ApiResponse> UpdateRoomAsync(int roomId, RoomRequest request, int adminId);
        Task<ApiResponse> UpdateRoomSettingAsync(RoomSettingRequest request, int adminId);
        Task<ApiResponse> GetRoomSettingsAsync(int roomId, int adminId);
        Task<ApiResponse> GetRoomServicesAsync(int roomId, int adminId);
        Task<ApiResponse> GetRoomOccupantsAsync(int roomId, int adminId);
    }
}
