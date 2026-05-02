using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class MotelService : IMotelService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _config;

        public MotelService(ApplicationDbContext db, IConfiguration config, INotificationService notificationService)
        {
            _db = db;
            _config = config;
            _notificationService = notificationService;
        }

        private async Task<bool> IsSuperuser(int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Role?.RoleName == "superuser";
        }

        private async Task<bool> IsAdminOfMotel(int adminId, int motelId)
        {
            if (await IsSuperuser(adminId)) return true; // Superuser có quyền với mọi khu trọ
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
            if (motel == null || (motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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
            var isSuper = await IsSuperuser(adminId);
            var motels = await _db.Motels
                .Include(m => m.Floors)
                .ThenInclude(f => f.Rooms)
                .ThenInclude(r => r.Setting)
                .Include(m => m.Floors)
                .ThenInclude(f => f.Rooms)
                .ThenInclude(r => r.Contracts)
                .Where(m => isSuper || m.OwnerUserId == adminId)
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
                        Status = r.Contracts != null && r.Contracts.Any(c => c.ContractStatus == "Active") ? "Occupied" : r.Status,
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
            if (floor == null || (floor.Motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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
            if (motel == null || (motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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
            if (room == null || (room.Motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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
            if (room == null || (room.Motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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



        public async Task<ApiResponse> GetRoomSettingsAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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
            if (room == null || (room.Motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var services = await _db.Services
                .Where(s => s.IsActive)
                .Select(s => new {
                    RoomServiceSettingId = 0, // No longer using specific setting IDs for global prices
                    s.ServiceId,
                    s.ServiceName,
                    s.Unit,
                    UnitPrice = s.DefaultPrice, // Always use Global DefaultPrice
                    s.CalculationType,
                    IsActive = true
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = services };
        }

        public async Task<ApiResponse> GetRoomOccupantsAsync(int roomId, int adminId)
        {
            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || (room.Motel.OwnerUserId != adminId && !await IsSuperuser(adminId)))
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

        // --- Global Services ---
        public async Task<ApiResponse> GetGlobalServicesAsync()
        {
            var services = await _db.Services
                .Where(s => s.IsSystemDefault && s.IsActive)
                .Select(s => new
                {
                    s.ServiceId,
                    s.ServiceName,
                    s.ServiceCode,
                    s.Unit,
                    s.DefaultPrice,
                    s.CalculationType
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = services };
        }

        public async Task<ApiResponse> CreateGlobalServiceAsync(ServiceRequest request, int adminId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user?.Role?.RoleName != "superuser")
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền tạo dịch vụ hệ thống." };

            var service = new Service
            {
                ServiceName = request.ServiceName,
                ServiceCode = request.ServiceCode,
                Unit = request.Unit,
                DefaultPrice = request.DefaultPrice,
                CalculationType = request.CalculationType,
                IsSystemDefault = true,
                IsActive = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Services.Add(service);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo dịch vụ hệ thống thành công.", Data = service };
        }

        public async Task<ApiResponse> UpdateGlobalServiceAsync(int serviceId, decimal defaultPrice, int adminId)
        {
            var service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.IsSystemDefault);
            if (service == null)
                return new ApiResponse { Success = false, Message = "Không tìm thấy dịch vụ." };

            service.DefaultPrice = defaultPrice;
            service.UpdatedAt = DateTime.Now;

            // Optional: Update all existing RoomServiceSetting that use this service
            // to keep the entire single-motel system synchronized.
            var roomSettings = await _db.RoomServiceSettings.Where(rs => rs.ServiceId == serviceId).ToListAsync();
            foreach(var rs in roomSettings)
            {
                rs.UnitPrice = defaultPrice;
                rs.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            // Gửi thông báo cho tất cả khách thuê
            string priceFormatted = defaultPrice.ToString("N0") + "đ";
            await _notificationService.CreateNotificationForRolesAsync(
                new[] { "Tenant" }, 
                "Cập nhật đơn giá dịch vụ", 
                $"Đơn giá {service.ServiceName} đã được cập nhật mới là {priceFormatted}. Mức giá này sẽ áp dụng cho kỳ hóa đơn tiếp theo.",
                "warning"
            );
            return new ApiResponse { Success = true, Message = "Đã cập nhật phí dịch vụ và gửi thông báo đến khách thuê thành công." };
        }

        public async Task<ApiResponse> SeedDefaultServicesAsync(int adminId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user?.Role?.RoleName != "superuser")
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền khởi tạo dịch vụ mẫu hệ thống." };

            var defaults = new List<Service>
            {
                new Service { ServiceName = "Tiền Điện", ServiceCode = "DIEN", Unit = "kWh", DefaultPrice = 3500, CalculationType = "metered", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now },
                new Service { ServiceName = "Tiền Nước", ServiceCode = "NUOC", Unit = "Khối", DefaultPrice = 25000, CalculationType = "metered", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now },
                new Service { ServiceName = "Phí Rác & Vệ sinh", ServiceCode = "RAC", Unit = "Phòng", DefaultPrice = 50000, CalculationType = "fixed", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now },
                new Service { ServiceName = "Internet / Wifi", ServiceCode = "WIFI", Unit = "Phòng", DefaultPrice = 100000, CalculationType = "fixed", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now }
            };

            foreach (var s in defaults)
            {
                if (!await _db.Services.AnyAsync(x => x.ServiceCode == s.ServiceCode && x.IsSystemDefault))
                {
                    _db.Services.Add(s);
                }
            }

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Đã khởi tạo các dịch vụ mẫu thành công (Điện 3.500, Nước 25.000...)." };
        }
    }
}
