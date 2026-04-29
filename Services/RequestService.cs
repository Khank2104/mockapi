using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class RequestService : IRequestService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;

        public RequestService(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse> CreateRequestAsync(CreateServiceRequest request, int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin khách thuê." };

            var occupant = await _db.RoomOccupants.FirstOrDefaultAsync(o => o.TenantId == tenant.TenantId && o.Status == "Staying");
            
            if (occupant == null) return new ApiResponse { Success = false, Message = "Bạn hiện không ở trong phòng nào để gửi yêu cầu." };

            var newRequest = new Request
            {
                TenantId = tenant.TenantId,
                RoomId = occupant.RoomId,
                Title = request.Title,
                Description = request.Description,
                RequestType = request.RequestType,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Requests.Add(newRequest);
            await _db.SaveChangesAsync();

            // Notify Admin
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == occupant.RoomId);
            if (room != null)
            {
                await _notificationService.CreateNotificationAsync(room.Motel.OwnerUserId, "Yêu cầu mới", $"Phòng {room.RoomCode} vừa gửi một yêu cầu: {request.Title}", "info");
            }

            return new ApiResponse { Success = true, Message = "Gửi yêu cầu thành công.", Data = newRequest };
        }

        public async Task<ApiResponse> GetAllRequestsAsync(int adminId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user == null || (user.Role.RoleName != "admin" && user.Role.RoleName != "superuser"))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var query = _db.Requests
                .Include(r => r.Tenant)
                .Include(r => r.Room)
                .ThenInclude(room => room.Motel)
                .AsQueryable();

            if (user.Role.RoleName == "admin")
            {
                query = query.Where(r => r.Room.Motel.OwnerUserId == adminId);
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt)
                .Select(r => new ServiceRequestResponse
                {
                    RequestId = r.RequestId,
                    Title = r.Title,
                    Description = r.Description,
                    RequestType = r.RequestType,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ResolutionNote = r.ResolutionNote,
                    TenantName = r.Tenant.FullName,
                    RoomCode = r.Room.RoomCode,
                    MotelName = r.Room.Motel.MotelName
                }).ToListAsync();

            return new ApiResponse { Success = true, Data = requests };
        }

        public async Task<ApiResponse> GetTenantRequestsAsync(int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin khách thuê." };

            var requests = await _db.Requests
                .Include(r => r.Room)
                .Where(r => r.TenantId == tenant.TenantId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ServiceRequestResponse
                {
                    RequestId = r.RequestId,
                    Title = r.Title,
                    Description = r.Description,
                    RequestType = r.RequestType,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ResolutionNote = r.ResolutionNote,
                    RoomCode = r.Room.RoomCode
                })
                .ToListAsync();
            return new ApiResponse { Success = true, Data = requests };
        }

        public async Task<ApiResponse> UpdateRequestStatusAsync(int requestId, UpdateRequestStatus request, int adminId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user == null || (user.Role.RoleName != "admin" && user.Role.RoleName != "superuser"))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var req = await _db.Requests.Include(r => r.Room).ThenInclude(room => room.Motel).FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (req == null) return new ApiResponse { Success = false, Message = "Yêu cầu không tồn tại." };

            if (user.Role.RoleName == "admin" && req.Room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Bạn không có quyền xử lý yêu cầu của khu trọ khác." };

            req.Status = request.Status;
            req.ResolutionNote = request.ResolutionNote;
            req.HandledBy = adminId;
            req.HandledAt = DateTime.Now;

            await _db.SaveChangesAsync();

            // Notify Tenant
            var tenantUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.Tenant.UserId);
            if (tenantUser != null)
            {
                await _notificationService.CreateNotificationAsync(tenantUser.UserId, "Cập nhật yêu cầu", $"Yêu cầu '{req.Title}' của bạn đã được cập nhật trạng thái: {request.Status}", "info");
            }

            return new ApiResponse { Success = true, Message = "Cập nhật trạng thái yêu cầu thành công." };
        }
    }
}
