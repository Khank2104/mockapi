using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class MotelService : IMotelService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAccessControlService _accessControl;

        public MotelService(ApplicationDbContext db, IAccessControlService accessControl)
        {
            _db = db;
            _accessControl = accessControl;
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
            if (motel == null || (motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
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
            try
            {
                var isSuper = await _accessControl.IsSuperuserAsync(adminId);
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
                    Floors = (m.Floors ?? new List<Floor>()).Select(f => new FloorResponse
                    {
                        FloorId = f.FloorId,
                        FloorNumber = f.FloorNumber,
                        FloorName = f.FloorName,
                        Status = f.Status,
                        Rooms = (f.Rooms ?? new List<Room>()).Select(r => new RoomResponse
                        {
                            RoomId = r.RoomId,
                            RoomCode = r.RoomCode,
                            Area = r.Area,
                            Status = r.Contracts != null && r.Contracts.Any(c => c.ContractStatus == "Active")
                                ? "Occupied"
                                : (r.Status == "Occupied" ? "Vacant" : r.Status),
                            Description = r.Description,
                            CurrentRent = r.Setting?.BaseRent
                        }).ToList()
                    }).ToList(),
                    Rooms = (m.Rooms ?? new List<Room>()).Where(r => r.FloorId == null).Select(r => new RoomResponse
                    {
                        RoomId = r.RoomId,
                        RoomCode = r.RoomCode,
                        Area = r.Area,
                        Status = r.Contracts != null && r.Contracts.Any(c => c.ContractStatus == "Active")
                            ? "Occupied"
                            : (r.Status == "Occupied" ? "Vacant" : r.Status),
                        Description = r.Description,
                        CurrentRent = r.Setting?.BaseRent
                    }).ToList()
                }).ToList();

                return new ApiResponse { Success = true, Data = response };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "GetMotelsByAdminAsync failed for adminId={AdminId}", adminId);
                return new ApiResponse { Success = false, Message = "Lỗi hệ thống: " + ex.Message };
            }
        }

        // --- Floor ---
        public async Task<ApiResponse> CreateFloorAsync(FloorRequest request, int adminId)
        {
            if (!await _accessControl.IsAdminOfMotelAsync(adminId, request.MotelId))
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
            if (floor == null || (floor.Motel.OwnerUserId != adminId && !await _accessControl.IsSuperuserAsync(adminId)))
                return new ApiResponse { Success = false, Message = "Không tìm thấy tầng hoặc bạn không có quyền." };

            floor.FloorNumber = request.FloorNumber;
            floor.FloorName = request.FloorName;
            floor.Status = request.Status;
            floor.Description = request.Description;
            floor.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật tầng thành công." };
        }

    }
}
