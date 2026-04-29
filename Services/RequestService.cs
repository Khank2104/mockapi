using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class RequestService : IRequestService
    {
        private readonly ApplicationDbContext _db;

        public RequestService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse> CreateRequestAsync(CreateTenantRequest request, int tenantUserId)
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
                // Filter requests by motel ownership
                query = query.Where(r => r.Room.Motel.OwnerUserId == adminId);
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return new ApiResponse { Success = true, Data = requests };
        }

        public async Task<ApiResponse> GetTenantRequestsAsync(int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin khách thuê." };

            var requests = await _db.Requests
                .Where(r => r.TenantId == tenant.TenantId)
                .OrderByDescending(r => r.CreatedAt)
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
            return new ApiResponse { Success = true, Message = "Cập nhật trạng thái yêu cầu thành công." };
        }
    }
}
