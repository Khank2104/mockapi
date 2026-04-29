using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IOccupancyService
    {
        // Occupant
        Task<ApiResponse> AddOccupantAsync(RoomOccupantRequest request, int adminId);
        Task<ApiResponse> RemoveOccupantAsync(int roomOccupantId, int adminId);
        
        // Contract
        Task<ApiResponse> CreateContractAsync(ContractRequest request, int adminId);
        Task<ApiResponse> TerminateContractAsync(int contractId, int adminId);
    }
}
