using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class OccupancyService : IOccupancyService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAccessControlService _accessControl;

        public OccupancyService(ApplicationDbContext db, IAccessControlService accessControl)
        {
            _db = db;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> AddOccupantAsync(RoomOccupantRequest request, int adminId)
        {
            var room = await _db.Rooms
                .Include(r => r.Motel)
                .Include(r => r.Setting)
                .Include(r => r.Occupants)
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId);

            if (room == null || !await _accessControl.IsAdminOfMotelAsync(adminId, room.MotelId))
                return new ApiResponse { Success = false, Message = "Không tìm thấy phòng hoặc bạn không có quyền." };

            if (room.Setting != null && room.Occupants.Count(o => o.Status == "Staying") >= room.Setting.MaxOccupants)
                return new ApiResponse { Success = false, Message = $"Phòng đã đạt số người ở tối đa ({room.Setting.MaxOccupants} người theo hợp đồng)." };

            // KIỂM TRA HỢP ĐỒNG: Phải có hợp đồng hiệu lực mới được thêm người ở
            var hasContract = await _db.Contracts.AnyAsync(c => c.RoomId == request.RoomId && c.ContractStatus == "Active");
            if (!hasContract)
                return new ApiResponse { Success = false, Message = "Phòng chưa có hợp đồng hiệu lực. Vui lòng ký hợp đồng trước khi thêm người vào ở." };

            if (await _db.RoomOccupants.AnyAsync(o => o.RoomId == request.RoomId && o.TenantId == request.TenantId && o.Status == "Staying"))
                return new ApiResponse { Success = false, Message = "Khách thuê này đã có trong phòng." };

            var occupant = new RoomOccupant
            {
                RoomId = request.RoomId,
                TenantId = request.TenantId,
                OccupantRole = request.OccupantRole,
                CheckInDate = request.CheckInDate,
                Status = "Staying",
                CreatedAt = DateTime.Now
            };

            _db.RoomOccupants.Add(occupant);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Thêm người ở vào phòng thành công." };
        }

        public async Task<ApiResponse> RemoveOccupantAsync(int roomOccupantId, int adminId)
        {
            var occupant = await _db.RoomOccupants
                .Include(o => o.Room)
                .ThenInclude(r => r.Motel)
                .FirstOrDefaultAsync(o => o.RoomOccupantId == roomOccupantId);

            if (occupant == null || !await _accessControl.IsAdminOfMotelAsync(adminId, occupant.Room.MotelId))
                return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin người ở hoặc bạn không có quyền." };

            occupant.Status = "MovedOut";
            occupant.CheckOutDate = DateTime.Now;
            occupant.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Đã xác nhận khách rời phòng." };
        }
    }
}
