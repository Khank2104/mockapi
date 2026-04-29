using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IMeterReadingService
    {
        Task<ApiResponse> CreateReadingAsync(CreateMeterReadingRequest request, int adminId);
        Task<ApiResponse> GetReadingsByRoomAsync(int roomId, int month, int year, int requesterId);
    }

    public interface IInvoiceCalculationService
    {
        // Calculates details and total without saving to DB
        Task<ApiResponse> CalculateMonthlyInvoiceAsync(int roomId, int month, int year);
    }

    public interface IInvoiceService
    {
        Task<ApiResponse> GenerateInvoiceAsync(GenerateInvoiceRequest request, int adminId);
        Task<ApiResponse> GetInvoiceByIdAsync(int invoiceId, int requesterId);
        Task<ApiResponse> GetInvoicesByRoomAsync(int roomId, int requesterId);
        Task<ApiResponse> GetInvoicesByTenantAsync(int tenantUserId);
    }

    public interface IPaymentService
    {
        Task<ApiResponse> CreatePaymentAsync(CreatePaymentRequest request, int adminId);
        Task<ApiResponse> GetPaymentsByInvoiceAsync(int invoiceId, int requesterId);
    }

    public interface IRequestService
    {
        Task<ApiResponse> CreateRequestAsync(CreateTenantRequest request, int tenantUserId);
        Task<ApiResponse> GetAllRequestsAsync(int adminId);
        Task<ApiResponse> GetTenantRequestsAsync(int tenantUserId);
        Task<ApiResponse> UpdateRequestStatusAsync(int requestId, UpdateRequestStatus request, int adminId);
    }
}
