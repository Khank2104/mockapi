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
        Task<ApiResponse> GetDashboardFinancialSummaryAsync(int month, int year, int adminId, int? motelId = null);
        Task<ApiResponse> GetRevenueChartDataAsync(int adminId, int? motelId = null);
        Task<ApiResponse> DeleteInvoiceAsync(int invoiceId, int adminId);
    }
}
