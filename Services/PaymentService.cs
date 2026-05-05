using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IAccessControlService _accessControl;

        public PaymentService(ApplicationDbContext db, INotificationService notificationService, IAccessControlService accessControl)
        {
            _db = db;
            _notificationService = notificationService;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> CreatePaymentAsync(CreatePaymentRequest request, int adminId)
        {
            // Only Admin/Superuser can record payments
            var requester = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (requester == null || (requester.Role.RoleName != "admin" && requester.Role.RoleName != "superuser"))
                return new ApiResponse { Success = false, Message = "Chỉ quản trị viên mới có quyền ghi nhận thanh toán." };

            if (!await _accessControl.CanAccessInvoiceAsync(request.InvoiceId, adminId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ hoặc hóa đơn không tồn tại." };

            var invoice = await _db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId);
            if (invoice == null) return new ApiResponse { Success = false, Message = "Hóa đơn không tồn tại." };

            if (request.Amount <= 0) return new ApiResponse { Success = false, Message = "Số tiền thanh toán phải lớn hơn 0." };

            decimal currentPaid = invoice.Payments.Sum(p => p.PaidAmount);
            decimal remaining = invoice.TotalAmount - currentPaid;
            
            if (request.Amount > remaining)
                return new ApiResponse { Success = false, Message = $"Số tiền thanh toán ({request.Amount}) không được vượt quá số tiền còn lại ({remaining})." };

            var payment = new Payment
            {
                InvoiceId = request.InvoiceId,
                PaidAmount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                PaymentDate = request.PaymentDate,
                Note = request.Note,
                ReceivedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Payments.Add(payment);
            
            decimal newPaidTotal = currentPaid + request.Amount;
            if (newPaidTotal >= invoice.TotalAmount)
                invoice.InvoiceStatus = "Paid";
            else if (newPaidTotal > 0)
                invoice.InvoiceStatus = "PartiallyPaid";

            await _db.SaveChangesAsync();

            // Notify Tenant
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == invoice.PrimaryTenantId);
            if (tenant != null && tenant.UserId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(tenant.UserId.Value, "Xác nhận thanh toán", $"Khoản thanh toán {request.Amount:N0}đ cho hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear} đã được ghi nhận.", "success");
            }

            return new ApiResponse { Success = true, Message = "Ghi nhận thanh toán thành công.", Data = payment };
        }

        public async Task<ApiResponse> GetPaymentsByInvoiceAsync(int invoiceId, int requesterId)
        {
            if (!await _accessControl.CanAccessInvoiceAsync(invoiceId, requesterId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var payments = await _db.Payments.Where(p => p.InvoiceId == invoiceId).ToListAsync();
            return new ApiResponse { Success = true, Data = payments };
        }
    }
}
