using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IInvoiceService
    {
        Task<ApiResponse> GenerateInvoiceAsync(GenerateInvoiceRequest request, int adminId);
        Task<ApiResponse> GetInvoiceByIdAsync(int invoiceId, int requesterId);
        Task<ApiResponse> GetInvoicesByRoomAsync(int roomId, int requesterId);
        Task<ApiResponse> GetInvoicesByTenantAsync(int tenantUserId);
        Task<ApiResponse> GetTenantRoomInfoAsync(int tenantUserId);
        Task<ApiResponse> GetBillingSummaryAsync(int month, int year, int adminId, int? motelId = null, int page = 1, int pageSize = 10);
        Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId, int requesterId);
        Task<ApiResponse> SubmitPaymentProofAsync(int invoiceId, string proofPath, int tenantUserId);
        Task<ApiResponse> VerifyPaymentAsync(int invoiceId, bool approved, int adminId);
    }
}
