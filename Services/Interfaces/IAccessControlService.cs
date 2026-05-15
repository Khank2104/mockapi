using System.Threading.Tasks;

namespace UserManagementSystem.Services
{
    public interface IAccessControlService
    {
        Task<bool> IsSuperuserAsync(int userId);
        Task<bool> IsAdminOfMotelAsync(int adminId, int motelId);
        Task<bool> IsAdminOrSuperAsync(int userId);
        Task<bool> CanAccessRoomAsync(int roomId, int userId);
        Task<bool> CanAccessInvoiceAsync(int invoiceId, int userId);
    }
}
