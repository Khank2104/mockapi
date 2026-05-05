using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IPaymentService
    {
        Task<ApiResponse> CreatePaymentAsync(CreatePaymentRequest request, int adminId);
        Task<ApiResponse> GetPaymentsByInvoiceAsync(int invoiceId, int requesterId);
    }
}
