using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class RoomManagementService : IRoomManagementService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAccessControlService _accessControl;

        public RoomManagementService(ApplicationDbContext db, IAccessControlService accessControl)
        {
            _db = db;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> CreateRoomAsync(RoomRequest request, int adminId)
        {
            var motel = await _db.Motels.FindAsync(request.MotelId);
            if (motel == null || (motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Không tìm thấy khu trọ hoặc bạn không có quyền." };

            if (motel.UseFloorManagement && !request.FloorId.HasValue)
                return new ApiResponse { Success = false, Message = "Khu trọ này có quản lý tầng, vui lòng chọn tầng cho phòng." };

            if (request.FloorId.HasValue)
            {
                var floor = await _db.Floors.FindAsync(request.FloorId.Value);
                if (floor == null || floor.MotelId != request.MotelId)
                    return new ApiResponse { Success = false, Message = "Tầng không thuộc khu trọ này." };
            }

            var room = new Room
            {
                MotelId = request.MotelId,
                FloorId = request.FloorId,
                RoomCode = request.RoomCode,
                Area = request.Area,
                Status = request.Status,
                Description = request.Description,
                CreatedAt = DateTime.Now
            };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            // Tự động gán các dịch vụ mặc định hệ thống
            var defaultServices = await _db.Services.Where(s => s.IsSystemDefault && s.IsActive).ToListAsync();
            foreach (var service in defaultServices)
            {
                _db.RoomServiceSettings.Add(new RoomServiceSetting
                {
                    RoomId = room.RoomId,
                    ServiceId = service.ServiceId,
                    UnitPrice = service.DefaultPrice,
                    CalculationType = service.CalculationType,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Tạo phòng thành công và đã gán các dịch vụ mặc định.", Data = room };
        }

        public async Task<ApiResponse> UpdateRoomAsync(int roomId, RoomRequest request, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Không tìm thấy phòng hoặc bạn không có quyền." };

            room.RoomCode = request.RoomCode;
            room.Area = request.Area;
            room.Status = request.Status;
            room.Description = request.Description;
            room.FloorId = request.FloorId;
            room.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật phòng thành công." };
        }

        public async Task<ApiResponse> DeleteRoomAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms
                .Include(r => r.Motel)
                .Include(r => r.Contracts)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null || (room.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Không tìm thấy phòng hoặc bạn không có quyền." };

            // Check if room is occupied
            var isOccupied = room.Contracts != null && room.Contracts.Any(c => c.ContractStatus == "Active");
            if (isOccupied)
                return new ApiResponse { Success = false, Message = "Không thể xóa phòng đang có người ở." };

            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Xóa phòng thành công." };
        }


        public async Task<ApiResponse> UpdateRoomSettingAsync(RoomSettingRequest request, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == request.RoomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };


            if (request.ExtraOccupantFee < 0)
                return new ApiResponse { Success = false, Message = "Phí phụ thu không hợp lệ." };

            var setting = await _db.RoomSettings.FirstOrDefaultAsync(s => s.RoomId == request.RoomId);
            if (setting == null)
            {
                setting = new RoomSetting { RoomId = request.RoomId };
                _db.RoomSettings.Add(setting);
            }

            setting.BaseRent = request.BaseRent;
            setting.DepositAmount = request.DepositAmount;
            setting.StandardOccupants = 2; // Cố định ngưỡng phụ thu là 2
            setting.MaxOccupants = request.StandardOccupants; // Lấy giá trị nhập từ UI làm giới hạn tối đa
            setting.ExtraOccupantFee = request.ExtraOccupantFee;
            setting.ApplyExtraOccupantFee = request.ApplyExtraOccupantFee;
            setting.EffectiveFrom = request.EffectiveFrom;
            setting.CreatedBy = adminId;
            setting.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật cấu hình phòng thành công." };
        }

        public async Task<ApiResponse> GetRoomSettingsAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var settings = await _db.RoomSettings.FirstOrDefaultAsync(s => s.RoomId == roomId);
            var activeContract = await _db.Contracts.FirstOrDefaultAsync(c => c.RoomId == roomId && c.ContractStatus == "Active");

            if (settings == null)
            {
                settings = new RoomSetting { RoomId = roomId };
            }

            // Đồng bộ dữ liệu tài chính từ hợp đồng đang hiệu lực (nếu có)
            if (activeContract != null)
            {
                settings.BaseRent = activeContract.MonthlyRent;
                settings.DepositAmount = activeContract.DepositAmount;
            }

            return new ApiResponse { Success = true, Data = settings };
        }

        public async Task<ApiResponse> GetRoomServicesAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            // Lấy danh sách dịch vụ hệ thống đang hoạt động
            var globalServices = await _db.Services.Where(s => s.IsActive).ToListAsync();
            
            // Lấy cấu hình dịch vụ riêng của phòng
            var roomSettings = await _db.RoomServiceSettings
                .Where(rs => rs.RoomId == roomId)
                .ToListAsync();

            var response = globalServices.Select(s => {
                var setting = roomSettings.FirstOrDefault(rs => rs.ServiceId == s.ServiceId);
                return new {
                    RoomServiceSettingId = setting?.RoomServiceSettingId ?? 0,
                    s.ServiceId,
                    s.ServiceName,
                    s.Unit,
                    UnitPrice = s.DefaultPrice, // Luôn ưu tiên giá global theo yêu cầu
                    s.CalculationType,
                    IsActive = setting?.IsActive ?? s.IsSystemDefault // Mặc định true nếu là dịch vụ hệ thống
                };
            }).ToList();

            return new ApiResponse { Success = true, Data = response };
        }

        public async Task<ApiResponse> GetRoomOccupantsAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var occupants = await _db.RoomOccupants
                .Include(o => o.Tenant)
                .Where(o => o.RoomId == roomId && o.CheckOutDate == null)
                .Select(o => new {
                    o.RoomOccupantId,
                    TenantName = o.Tenant.FullName,
                    Role = o.OccupantRole,
                    o.CheckInDate,
                    o.Tenant.Phone
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = occupants };
        }
    }
}
