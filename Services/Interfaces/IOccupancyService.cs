using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IOccupancyService
    {
        // Occupant
        Task<ApiResponse> AddOccupantAsync(RoomOccupantRequest request, int adminId);
        Task<ApiResponse> RemoveOccupantAsync(int roomOccupantId, int adminId);
    }
}
