using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IRequestService
    {
        Task<ApiResponse> CreateRequestAsync(CreateServiceRequest request, int tenantUserId);
        Task<ApiResponse> GetAllRequestsAsync(int adminId);
        Task<ApiResponse> GetTenantRequestsAsync(int tenantUserId);
        Task<ApiResponse> UpdateRequestStatusAsync(int requestId, UpdateRequestStatus request, int adminId);
    }
}
