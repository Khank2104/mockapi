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
            
            // Cập nhật trạng thái phòng sang Occupied
            room.Status = "Occupied";
            await _db.SaveChangesAsync();

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

            int roomId = contract.RoomId;
            
            // Xóa tất cả Invoices liên quan đến hợp đồng này
            var invoices = await _db.Invoices.Where(i => i.ContractId == contractId).ToListAsync();
            _db.Invoices.RemoveRange(invoices);

            // Tìm tất cả người ở trong phòng
            var occupants = await _db.RoomOccupants.Where(o => o.RoomId == roomId).ToListAsync();
            var tenantIds = occupants.Select(o => o.TenantId).ToList();

            // Xóa tất cả yêu cầu (Requests) của các khách thuê này
            if (tenantIds.Any()) {
                var requests = await _db.Requests.Where(r => tenantIds.Contains(r.TenantId)).ToListAsync();
                _db.Requests.RemoveRange(requests);
            }

            // Xóa RoomOccupants
            _db.RoomOccupants.RemoveRange(occupants);

            // Xóa Hợp đồng
            _db.Contracts.Remove(contract);

            // Xóa Khách thuê (Tenants)
            var tenants = await _db.Tenants.Where(t => tenantIds.Contains(t.TenantId)).ToListAsync();
            var userIds = tenants.Where(t => t.UserId != null).Select(t => t.UserId.Value).ToList();
            _db.Tenants.RemoveRange(tenants);

            // Xóa Tài khoản Khách thuê (Users)
            if (userIds.Any())
            {
                var users = await _db.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();
                _db.Users.RemoveRange(users);
            }

            // Đặt phòng lại trạng thái Trống (Vacant)
            var room = await _db.Rooms.FindAsync(roomId);
            if (room != null)
            {
                room.Status = "Vacant";
            }

            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Đã chấm dứt hợp đồng và dọn sạch dữ liệu khách thuê." };
        }

        public async Task<ApiResponse> GetAllContractsAsync(int adminId)
        {
            var contracts = await _db.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Motel)
                .Include(c => c.PrimaryTenant)
                .Where(c => c.Room.Motel.OwnerUserId == adminId || _db.Users.Any(u => u.UserId == adminId && u.Role.RoleName == "superuser"))
                .Select(c => new
                {
                    c.ContractId,
                    c.RoomId,
                    RoomCode = c.Room.RoomCode,
                    TenantName = c.PrimaryTenant.FullName,
                    c.StartDate,
                    c.EndDate,
                    c.MonthlyRent,
                    c.DepositAmount,
                    c.ContractStatus
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = contracts };
        }
    }
}
