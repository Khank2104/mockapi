using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class MotelService : IMotelService
    {
        private readonly ApplicationDbContext _db;

        public MotelService(ApplicationDbContext db)
        {
            _db = db;
        }

        private async Task<bool> IsAdminOfMotel(int adminId, int motelId)
        {
            return await _db.Motels.AnyAsync(m => m.MotelId == motelId && m.OwnerUserId == adminId);
        }

        // --- Motel ---
        public async Task<ApiResponse> CreateMotelAsync(MotelRequest request, int adminId)
        {
            var motel = new Motel
            {
                MotelName = request.MotelName,
                Address = request.Address,
                Description = request.Description,
                OwnerUserId = adminId,
                UseFloorManagement = request.UseFloorManagement,
                Status = "Active",
                CreatedAt = DateTime.Now
            };
            _db.Motels.Add(motel);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo khu trọ thành công.", Data = motel };
        }

        public async Task<ApiResponse> UpdateMotelAsync(int motelId, MotelRequest request, int adminId)
        {
            var motel = await _db.Motels.FindAsync(motelId);
            if (motel == null || motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Không tìm thấy khu trọ hoặc bạn không có quyền." };

            motel.MotelName = request.MotelName;
            motel.Address = request.Address;
            motel.Description = request.Description;
            motel.UseFloorManagement = request.UseFloorManagement;
            motel.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật khu trọ thành công." };
        }

        public async Task<ApiResponse> GetMotelsByAdminAsync(int adminId)
        {
            var motels = await _db.Motels.Where(m => m.OwnerUserId == adminId).ToListAsync();
            return new ApiResponse { Success = true, Data = motels };
        }

        // --- Floor ---
        public async Task<ApiResponse> CreateFloorAsync(FloorRequest request, int adminId)
        {
            if (!await IsAdminOfMotel(adminId, request.MotelId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var floor = new Floor
            {
                MotelId = request.MotelId,
                FloorNumber = request.FloorNumber,
                FloorName = request.FloorName,
                Status = request.Status,
                Description = request.Description,
                CreatedAt = DateTime.Now
            };
            _db.Floors.Add(floor);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo tầng thành công.", Data = floor };
        }

        public async Task<ApiResponse> UpdateFloorAsync(int floorId, FloorRequest request, int adminId)
        {
            var floor = await _db.Floors.Include(f => f.Motel).FirstOrDefaultAsync(f => f.FloorId == floorId);
            if (floor == null || floor.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Không tìm thấy tầng hoặc bạn không có quyền." };

            floor.FloorNumber = request.FloorNumber;
            floor.FloorName = request.FloorName;
            floor.Status = request.Status;
            floor.Description = request.Description;
            floor.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật tầng thành công." };
        }

        // --- Room ---
        public async Task<ApiResponse> CreateRoomAsync(RoomRequest request, int adminId)
        {
            if (!await IsAdminOfMotel(adminId, request.MotelId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

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
            return new ApiResponse { Success = true, Message = "Tạo phòng thành công.", Data = room };
        }

        public async Task<ApiResponse> UpdateRoomAsync(int roomId, RoomRequest request, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.Motel.OwnerUserId != adminId)
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

        // --- Settings ---
        public async Task<ApiResponse> UpdateRoomSettingAsync(RoomSettingRequest request, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == request.RoomId);
            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            if (request.StandardOccupants > request.MaxOccupants)
                return new ApiResponse { Success = false, Message = "Số người tiêu chuẩn không được vượt quá số người tối đa." };

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
            setting.StandardOccupants = request.StandardOccupants;
            setting.MaxOccupants = request.MaxOccupants;
            setting.ExtraOccupantFee = request.ExtraOccupantFee;
            setting.ApplyExtraOccupantFee = request.ApplyExtraOccupantFee;
            setting.EffectiveFrom = request.EffectiveFrom;
            setting.CreatedBy = adminId;
            setting.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật cấu hình phòng thành công." };
        }

        public async Task<ApiResponse> UpdateRoomServiceSettingAsync(RoomServiceSettingRequest request, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == request.RoomId);
            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var setting = await _db.RoomServiceSettings
                .FirstOrDefaultAsync(s => s.RoomId == request.RoomId && s.ServiceId == request.ServiceId);

            if (setting == null)
            {
                setting = new RoomServiceSetting
                {
                    RoomId = request.RoomId,
                    ServiceId = request.ServiceId,
                    CreatedAt = DateTime.Now
                };
                _db.RoomServiceSettings.Add(setting);
            }

            setting.UnitPrice = request.UnitPrice;
            setting.CalculationType = request.CalculationType;
            setting.IsActive = request.IsActive;
            setting.Note = request.Note;
            setting.CreatedBy = adminId;
            setting.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật dịch vụ cho phòng thành công." };
        }
    }
}
