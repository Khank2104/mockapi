using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IMeterReadingService
    {
        Task<ApiResponse> CreateReadingAsync(CreateMeterReadingRequest request, int adminId);
        Task<ApiResponse> GetReadingsByRoomAsync(int roomId, int month, int year, int requesterId);
        Task<ApiResponse> GetLatestReadingsAsync(int roomId, int requesterId);
        Task<ApiResponse> DeleteReadingAsync(int readingId, int adminId);
    }
}
