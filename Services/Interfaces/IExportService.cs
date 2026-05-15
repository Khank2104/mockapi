namespace UserManagementSystem.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId, int requesterId);
    }
}
