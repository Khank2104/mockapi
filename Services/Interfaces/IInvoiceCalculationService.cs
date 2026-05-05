using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IInvoiceCalculationService
    {
        // Calculates details and total without saving to DB
        Task<ApiResponse> CalculateMonthlyInvoiceAsync(int roomId, int month, int year);
    }
}
