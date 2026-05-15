using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IPaymentService
    {
        Task<ApiResponse> CreatePaymentAsync(CreatePaymentRequest request, int adminId);
        Task<ApiResponse> GetPaymentsByInvoiceAsync(int invoiceId, int requesterId);
        Task<ApiResponse> SubmitPaymentProofAsync(int invoiceId, string proofPath, int tenantUserId);
        Task<ApiResponse> VerifyPaymentAsync(int invoiceId, bool approved, int adminId, decimal? actualAmount = null);
    }
}
