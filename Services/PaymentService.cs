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

        public async Task<ApiResponse> SubmitPaymentProofAsync(int invoiceId, string proofPath, int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy khách thuê." };

            var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice == null) return new ApiResponse { Success = false, Message = "Hóa đơn không tồn tại." };

            // Kiểm tra xem khách có ở trong phòng của hóa đơn này không
            var isOccupant = await _db.RoomOccupants.AnyAsync(o => o.RoomId == invoice.RoomId && o.TenantId == tenant.TenantId && o.Status == "Staying");
            if (!isOccupant && invoice.PrimaryTenantId != tenant.TenantId)
                return new ApiResponse { Success = false, Message = "Bạn không có quyền gửi minh chứng cho hóa đơn này." };

            invoice.PaymentProofPath = proofPath;
            invoice.InvoiceStatus = "Pending";
            invoice.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            // Notify Admin/Owner
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == invoice.RoomId);
            if (room != null)
            {
                await _notificationService.CreateNotificationAsync(room.Motel.OwnerUserId, "Xác nhận thanh toán mới", $"Phòng {room.RoomCode} đã gửi minh chứng thanh toán. Vui lòng kiểm tra đối soát.", "info");
            }

            return new ApiResponse { Success = true, Message = "Đã gửi minh chứng thanh toán thành công. Vui lòng chờ Admin xác nhận." };
        }

        public async Task<ApiResponse> VerifyPaymentAsync(int invoiceId, bool approved, int adminId, decimal? actualAmount = null)
        {
            var invoice = await _db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice == null) return new ApiResponse { Success = false, Message = "Hóa đơn không tồn tại." };

            if (!await _accessControl.CanAccessInvoiceAsync(invoiceId, adminId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            if (invoice.InvoiceStatus != "Pending")
                return new ApiResponse { Success = false, Message = "Hóa đơn không ở trạng thái chờ duyệt." };

            if (approved)
            {
                decimal alreadyPaid = invoice.Payments.Sum(p => p.PaidAmount);
                decimal remaining = invoice.TotalAmount - alreadyPaid;
                decimal amountToRecord = actualAmount ?? remaining;

                if (amountToRecord <= 0)
                    return new ApiResponse { Success = false, Message = "Hóa đơn này thực tế đã được trả đủ trước đó." };

                // Tạo bản ghi thanh toán
                var payment = new Payment
                {
                    InvoiceId = invoice.InvoiceId,
                    PaidAmount = amountToRecord,
                    PaymentMethod = "Transfer",
                    PaymentDate = DateTime.Now,
                    Note = "Thanh toán qua QR (Duyệt minh chứng)",
                    ReceivedBy = adminId,
                    CreatedAt = DateTime.Now
                };
                _db.Payments.Add(payment);

                // Lấy thông tin khách thuê để cập nhật số dư và gửi thông báo
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == invoice.PrimaryTenantId);

                // Cập nhật trạng thái hóa đơn
                if (alreadyPaid + amountToRecord >= invoice.TotalAmount)
                {
                    invoice.InvoiceStatus = "Paid";
                    // Nếu trả dư, cộng vào Balance cho khách
                    decimal surplus = (alreadyPaid + amountToRecord) - invoice.TotalAmount;
                    if (surplus > 0 && tenant != null)
                    {
                        tenant.Balance += surplus;
                        tenant.UpdatedAt = DateTime.Now;
                    }
                }
                else
                {
                    invoice.InvoiceStatus = "PartiallyPaid";
                }
                
                // Notify Tenant
                if (tenant != null && tenant.UserId.HasValue)
                {
                    string msg = (invoice.InvoiceStatus == "Paid") 
                        ? $"Hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear} đã được phê duyệt thanh toán đủ."
                        : $"Đã ghi nhận thanh toán một phần {amountToRecord:N0}đ cho hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear}.";
                    await _notificationService.CreateNotificationAsync(tenant.UserId.Value, "Xác nhận thanh toán", msg, "success");
                }
            }
            else
            {
                invoice.InvoiceStatus = "Unpaid";
                invoice.PaymentProofPath = null; // Xóa minh chứng cũ để khách gửi lại
                
                // Notify Tenant
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == invoice.PrimaryTenantId);
                if (tenant != null && tenant.UserId.HasValue)
                {
                    await _notificationService.CreateNotificationAsync(tenant.UserId.Value, "Từ chối thanh toán", $"Minh chứng thanh toán cho hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear} không hợp lệ. Vui lòng kiểm tra lại.", "warning");
                }
            }

            invoice.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = approved ? "Đã phê duyệt thanh toán." : "Đã từ chối minh chứng thanh toán." };
        }
    }
}
