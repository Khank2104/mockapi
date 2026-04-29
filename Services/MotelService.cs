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
            var motels = await _db.Motels
                .Include(m => m.Floors)
                .ThenInclude(f => f.Rooms)
                .ThenInclude(r => r.Setting)
                .Where(m => m.OwnerUserId == adminId)
                .ToListAsync();

            var response = motels.Select(m => new MotelResponse
            {
                MotelId = m.MotelId,
                MotelName = m.MotelName,
                Address = m.Address,
                Description = m.Description,
                UseFloorManagement = m.UseFloorManagement,
                Status = m.Status,
                Floors = m.Floors.Select(f => new FloorResponse
                {
                    FloorId = f.FloorId,
                    FloorNumber = f.FloorNumber,
                    FloorName = f.FloorName,
                    Status = f.Status,
                    Rooms = f.Rooms.Select(r => new RoomResponse
                    {
                        RoomId = r.RoomId,
                        RoomCode = r.RoomCode,
                        Area = r.Area,
                        Status = r.Status,
                        Description = r.Description,
                        CurrentRent = r.Setting?.BaseRent
                    }).ToList()
                }).ToList()
            }).ToList();

            return new ApiResponse { Success = true, Data = response };
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
            var motel = await _db.Motels.FindAsync(request.MotelId);
            if (motel == null || motel.OwnerUserId != adminId)
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

        public async Task<ApiResponse> GetRoomSettingsAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var settings = await _db.RoomSettings.FirstOrDefaultAsync(s => s.RoomId == roomId);
            return new ApiResponse { Success = true, Data = settings };
        }

        public async Task<ApiResponse> GetRoomServicesAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var services = await _db.RoomServiceSettings
                .Include(s => s.Service)
                .Where(s => s.RoomId == roomId)
                .Select(s => new {
                    s.RoomServiceSettingId,
                    s.ServiceId,
                    s.Service.ServiceName,
                    s.Service.Unit,
                    s.UnitPrice,
                    s.CalculationType,
                    s.IsActive
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = services };
        }

        public async Task<ApiResponse> GetRoomOccupantsAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var occupants = await _db.RoomOccupants
                .Include(o => o.Tenant)
                .Where(o => o.RoomId == roomId && o.CheckOutDate == null)
                .Select(o => new {
                    o.RoomOccupantId,
                    o.Tenant.FullName,
                    o.OccupantRole,
                    o.CheckInDate,
                    o.Tenant.Phone
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = occupants };
        }
    }
}
