using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IContractService
    {
        Task<ApiResponse> CreateContractAsync(ContractRequest request, int adminId);
        Task<ApiResponse> TerminateContractAsync(int contractId, int adminId);
        Task<ApiResponse> GetAllContractsAsync(int adminId, int? motelId = null, int page = 1, int pageSize = 10);
        Task<ApiResponse> GetActiveContractByRoomAsync(int roomId);
        Task<ApiResponse> GetContractForPrintAsync(int contractId, int adminId);
        Task<ApiResponse> UpdateContractAsync(int contractId, ContractRequest request, int adminId);
    }
}
