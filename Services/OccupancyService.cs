using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class OccupancyService : IOccupancyService
    {
        private readonly ApplicationDbContext _db;

        public OccupancyService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse> AddOccupantAsync(RoomOccupantRequest request, int adminId)
        {
            var room = await _db.Rooms
                .Include(r => r.Motel)
                .Include(r => r.Setting)
                .Include(r => r.Occupants)
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId);

            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Không tìm thấy phòng hoặc bạn không có quyền." };

            if (room.Setting != null && room.Occupants.Count(o => o.Status == "Staying") >= room.Setting.MaxOccupants)
                return new ApiResponse { Success = false, Message = "Phòng đã đạt số người ở tối đa." };

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

            if (occupant == null || occupant.Room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin người ở hoặc bạn không có quyền." };

            occupant.Status = "MovedOut";
            occupant.CheckOutDate = DateTime.Now;
            occupant.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Đã xác nhận khách rời phòng." };
        }

        public async Task<ApiResponse> CreateContractAsync(ContractRequest request, int adminId)
        {
            var room = await _db.Rooms
                .Include(r => r.Motel)
                .Include(r => r.Floor)
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId);

            if (room == null || room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            if (room.Floor != null && (room.Floor.Status == "Inactive" || room.Floor.Status == "Maintenance"))
                return new ApiResponse { Success = false, Message = "Không thể tạo hợp đồng cho phòng thuộc tầng đang bảo trì hoặc ngừng hoạt động." };

            // Kiểm tra Primary Tenant có trong danh sách người ở không
            bool isOccupant = await _db.RoomOccupants.AnyAsync(o => 
                o.RoomId == request.RoomId && o.TenantId == request.PrimaryTenantId && o.Status == "Staying");
            
            if (!isOccupant)
                return new ApiResponse { Success = false, Message = "Người đại diện hợp đồng phải có tên trong danh sách người ở của phòng." };

            var contract = new Contract
            {
                RoomId = request.RoomId,
                PrimaryTenantId = request.PrimaryTenantId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MonthlyRent = request.MonthlyRent,
                DepositAmount = request.DepositAmount,
                ContractStatus = "Active",
                Terms = request.Terms,
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Contracts.Add(contract);
            await _db.SaveChangesAsync();

            // Cập nhật trạng thái phòng sang Occupied nếu đang Available
            if (room.Status == "Available")
            {
                room.Status = "Occupied";
                await _db.SaveChangesAsync();
            }

            return new ApiResponse { Success = true, Message = "Tạo hợp đồng thành công.", Data = contract };
        }

        public async Task<ApiResponse> TerminateContractAsync(int contractId, int adminId)
        {
            var contract = await _db.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Motel)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null || contract.Room.Motel.OwnerUserId != adminId)
                return new ApiResponse { Success = false, Message = "Không tìm thấy hợp đồng hoặc quyền hạn không đủ." };

            contract.ContractStatus = "Terminated";
            contract.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Đã chấm dứt hợp đồng." };
        }
    }
}
