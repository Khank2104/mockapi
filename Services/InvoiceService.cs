using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInvoiceCalculationService _calcService;
        private readonly INotificationService _notificationService;

        public InvoiceService(ApplicationDbContext db, IInvoiceCalculationService calcService, INotificationService notificationService)
        {
            _db = db;
            _calcService = calcService;
            _notificationService = notificationService;
        }

        private async Task<bool> CanAccessRoom(int roomId, int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;
            if (user.Role.RoleName == "superuser") return true;

            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return false;

            if (user.Role.RoleName == "admin")
            {
                return room.Motel.OwnerUserId == userId;
            }

            if (user.Role.RoleName == "tenant")
            {
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tenant == null) return false;
                return await _db.RoomOccupants.AnyAsync(o => o.RoomId == roomId && o.TenantId == tenant.TenantId && o.Status == "Staying");
            }

            return false;
        }

        public async Task<ApiResponse> GenerateInvoiceAsync(GenerateInvoiceRequest request, int adminId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user == null || (user.Role.RoleName != "admin" && user.Role.RoleName != "superuser"))
                return new ApiResponse { Success = false, Message = "Chỉ quản trị viên mới có quyền phát hành hóa đơn." };

            if (!await CanAccessRoom(request.RoomId, adminId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ hoặc phòng không tồn tại." };

            if (await _db.Invoices.AnyAsync(i => i.RoomId == request.RoomId && i.BillingMonth == request.BillingMonth && i.BillingYear == request.BillingYear))
                return new ApiResponse { Success = false, Message = "Hóa đơn kỳ này đã tồn tại." };

            var calcResult = await _calcService.CalculateMonthlyInvoiceAsync(request.RoomId, request.BillingMonth, request.BillingYear);
            if (!calcResult.Success) return calcResult;

            dynamic calcData = calcResult.Data!;
            List<InvoiceDetailResponse> details = calcData.Details;

            var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.RoomId == request.RoomId && c.ContractStatus == "Active");

            var invoice = new Invoice
            {
                RoomId = request.RoomId,
                BillingMonth = request.BillingMonth,
                BillingYear = request.BillingYear,
                RoomRent = calcData.RoomRent,
                OccupantCount = calcData.OccupantCount,
                ExtraOccupantCount = calcData.ExtraOccupantCount,
                ExtraOccupantTotal = calcData.ExtraOccupantTotal,
                ServiceTotal = calcData.ServiceTotal,
                TotalAmount = calcData.TotalAmount,
                InvoiceStatus = "Unpaid",
                DueDate = request.DueDate,
                ContractId = contract?.ContractId,
                PrimaryTenantId = contract?.PrimaryTenantId ?? 0,
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            foreach (var d in details)
            {
                var service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceName == d.ServiceName);
                _db.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ServiceId = service?.ServiceId,
                    ItemName = d.ServiceName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.SubTotal,
                    Note = d.Description
                });
            }

            await _db.SaveChangesAsync();

            // Notify Tenants in the room
            var room = await _db.Rooms.Include(r => r.Occupants).ThenInclude(o => o.Tenant).FirstOrDefaultAsync(r => r.RoomId == invoice.RoomId);
            if (room != null)
            {
                foreach (var occupant in room.Occupants.Where(o => o.Status == "Staying" && o.Tenant.UserId.HasValue))
                {
                    await _notificationService.CreateNotificationAsync(occupant.Tenant.UserId!.Value, "Hóa đơn mới", $"Hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear} đã được phát hành.", "info");
                }
            }

            return new ApiResponse { Success = true, Message = "Phát hành hóa đơn thành công.", Data = invoice.InvoiceId };
        }

        public async Task<ApiResponse> GetInvoiceByIdAsync(int invoiceId, int requesterId)
        {
            var i = await _db.Invoices
                .Include(i => i.Room)
                .Include(i => i.Details)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (i == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hóa đơn." };

            if (!await CanAccessRoom(i.RoomId, requesterId))
                return new ApiResponse { Success = false, Message = "Bạn không có quyền xem hóa đơn này." };

            var response = new InvoiceResponse
            {
                InvoiceId = i.InvoiceId,
                RoomId = i.RoomId,
                RoomCode = i.Room.RoomCode,
                BillingMonth = i.BillingMonth,
                BillingYear = i.BillingYear,
                RoomRent = i.RoomRent,
                ServiceTotal = i.ServiceTotal,
                ExtraOccupantTotal = i.ExtraOccupantTotal,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.Payments.Sum(p => p.PaidAmount),
                Status = i.InvoiceStatus,
                DueDate = i.DueDate,
                Details = i.Details.Select(d => new InvoiceDetailResponse
                {
                    ServiceName = d.ItemName,
                    Description = d.Note ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    SubTotal = d.Amount
                }).ToList()
            };

            return new ApiResponse { Success = true, Data = response };
        }

        public async Task<ApiResponse> GetInvoicesByRoomAsync(int roomId, int requesterId)
        {
            if (!await CanAccessRoom(roomId, requesterId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var invoices = await _db.Invoices
                .Include(i => i.Payments)
                .Where(i => i.RoomId == roomId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            
            var result = invoices.Select(i => new {
                i.InvoiceId,
                i.BillingMonth,
                i.BillingYear,
                i.TotalAmount,
                PaidAmount = i.Payments.Sum(p => p.PaidAmount),
                i.InvoiceStatus,
                i.CreatedAt
            });

            return new ApiResponse { Success = true, Data = result };
        }

        public async Task<ApiResponse> GetInvoicesByTenantAsync(int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin khách thuê." };

            var roomIds = await _db.RoomOccupants
                .Where(o => o.TenantId == tenant.TenantId && o.Status == "Staying")
                .Select(o => o.RoomId)
                .ToListAsync();

            var invoices = await _db.Invoices
                .Include(i => i.Payments)
                .Where(i => i.PrimaryTenantId == tenant.TenantId || roomIds.Contains(i.RoomId))
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .ToListAsync();

            var result = invoices.Select(i => new {
                i.InvoiceId,
                i.BillingMonth,
                i.BillingYear,
                i.TotalAmount,
                PaidAmount = i.Payments.Sum(p => p.PaidAmount),
                i.InvoiceStatus,
                i.DueDate
            });

            return new ApiResponse { Success = true, Data = result };
        }

        public async Task<ApiResponse> GetTenantRoomInfoAsync(int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ khách thuê." };

            var occupant = await _db.RoomOccupants
                .Include(o => o.Room).ThenInclude(r => r.Motel)
                .Include(o => o.Room).ThenInclude(r => r.Setting)
                .FirstOrDefaultAsync(o => o.TenantId == tenant.TenantId && o.Status == "Staying");

            if (occupant == null) return new ApiResponse { Success = false, Message = "Bạn hiện chưa ở phòng nào." };

            var room = occupant.Room;
            return new ApiResponse { 
                Success = true, 
                Data = new {
                    MotelName = room.Motel.MotelName,
                    RoomCode = room.RoomCode,
                    Area = room.Area,
                    MonthlyRent = room.Setting?.BaseRent ?? 0,
                    CheckInDate = occupant.CheckInDate
                }
            };
        }
    }
}
