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

            // Kiểm tra xem phòng đã có hợp đồng nào đang Active chưa
            var existingContract = await _db.Contracts.AnyAsync(c => c.RoomId == request.RoomId && c.ContractStatus == "Active");
            if (existingContract)
                return new ApiResponse { Success = false, Message = "Phòng đã có hợp đồng đang hoạt động. Vui lòng chấm dứt hợp đồng cũ trước." };

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
            
            // TỰ ĐỘNG THÊM NGƯỜI ĐẠI DIỆN VÀO DANH SÁCH NGƯỜI Ở
            var primaryOccupant = new RoomOccupant
            {
                RoomId = request.RoomId,
                TenantId = request.PrimaryTenantId,
                OccupantRole = "Owner",
                CheckInDate = request.StartDate,
                Status = "Staying",
                CreatedAt = DateTime.Now
            };
            _db.RoomOccupants.Add(primaryOccupant);

            // CẬP NHẬT CẤU HÌNH PHÒNG (ROOM SETTINGS) TỪ HỢP ĐỒNG
            var roomSetting = await _db.RoomSettings.FirstOrDefaultAsync(rs => rs.RoomId == request.RoomId);
            if (roomSetting == null)
            {
                roomSetting = new RoomSetting { RoomId = request.RoomId };
                _db.RoomSettings.Add(roomSetting);
            }
            roomSetting.BaseRent = request.MonthlyRent;
            roomSetting.DepositAmount = request.DepositAmount;
            
            // Lưu thiết lập số người và phụ thu
            // Theo yêu cầu: Ngưỡng tính phụ thu luôn là 2 (StandardOccupants = 2)
            // Giá trị nhập từ UI sẽ là Giới hạn tối đa (MaxOccupants)
            roomSetting.StandardOccupants = 2; 
            roomSetting.MaxOccupants = request.StandardOccupants; // Giá trị này từ ô "Số người tối đa" ở UI
            roomSetting.ExtraOccupantFee = request.ExtraOccupantFee;
            roomSetting.ApplyExtraOccupantFee = request.ExtraOccupantFee > 0;
            
            roomSetting.UpdatedAt = DateTime.Now;

            // CẬP NHẬT DỊCH VỤ CỦA PHÒNG
            var globalServices = await _db.Services.Where(s => s.IsActive).ToListAsync();
            var roomServiceSettings = await _db.RoomServiceSettings.Where(rs => rs.RoomId == request.RoomId).ToListAsync();
            
            foreach (var service in globalServices)
            {
                var setting = roomServiceSettings.FirstOrDefault(rs => rs.ServiceId == service.ServiceId);
                if (setting == null)
                {
                    setting = new RoomServiceSetting
                    {
                        RoomId = request.RoomId,
                        ServiceId = service.ServiceId,
                        UnitPrice = service.DefaultPrice,
                        CalculationType = service.CalculationType,
                        CreatedAt = DateTime.Now
                    };
                    _db.RoomServiceSettings.Add(setting);
                }

                string sName = service.ServiceName?.ToLower() ?? "";
                bool isMandatory = sName.Contains("điện") || sName.Contains("nước");

                if (isMandatory)
                {
                    setting.IsActive = true; // Luôn bật Điện/Nước
                }
                else
                {
                    setting.IsActive = request.SelectedServiceIds != null && request.SelectedServiceIds.Contains(service.ServiceId);
                }
                setting.UpdatedAt = DateTime.Now;
            }

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
            
            // 1. Chuyển trạng thái hợp đồng sang Terminated
            contract.ContractStatus = "Terminated";
            contract.EndDate = DateTime.Now;
            contract.UpdatedAt = DateTime.Now;

            // 2. Chuyển trạng thái tất cả người ở trong phòng sang MovedOut
            var occupants = await _db.RoomOccupants.Where(o => o.RoomId == roomId && o.Status == "Staying").ToListAsync();
            foreach (var occupant in occupants)
            {
                occupant.Status = "MovedOut";
                occupant.CheckOutDate = DateTime.Now;
                occupant.UpdatedAt = DateTime.Now;
            }

            // 3. Vô hiệu hóa tài khoản của các khách thuê (nếu có)
            var tenantIds = occupants.Select(o => o.TenantId).ToList();
            var tenants = await _db.Tenants.Where(t => tenantIds.Contains(t.TenantId)).ToListAsync();
            foreach (var tenant in tenants)
            {
                tenant.TenantStatus = "MovedOut";
                if (tenant.UserId.HasValue)
                {
                    var user = await _db.Users.FindAsync(tenant.UserId.Value);
                    if (user != null)
                    {
                        user.Status = "Inactive";
                    }
                }
            }

            // 4. Đặt phòng lại trạng thái Trống (Vacant)
            var room = await _db.Rooms.FindAsync(roomId);
            if (room != null)
            {
                room.Status = "Vacant";
                room.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Đã chấm dứt hợp đồng thành công. Dữ liệu cũ đã được lưu trữ vào lịch sử." };
        }

        public async Task<ApiResponse> GetAllContractsAsync(int adminId, int? motelId = null, int page = 1, int pageSize = 10)
        {
            var isSuper = await _db.Users.AnyAsync(u => u.UserId == adminId && u.Role.RoleName == "superuser");

            var query = _db.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Motel)
                .Include(c => c.PrimaryTenant)
                .Where(c => (isSuper || c.Room.Motel.OwnerUserId == adminId) 
                            && (c.ContractStatus == "Active" || c.ContractStatus == "Waiting") 
                            && c.ContractStatus != "Terminated");

            if (motelId.HasValue && motelId.Value > 0)
            {
                query = query.Where(c => c.Room.MotelId == motelId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.ContractId,
                    c.RoomId,
                    RoomCode = c.Room.RoomCode,
                    MotelName = c.Room.Motel.MotelName,
                    TenantName = c.PrimaryTenant.FullName,
                    c.StartDate,
                    c.EndDate,
                    c.MonthlyRent,
                    c.DepositAmount,
                    c.ContractStatus
                })
                .ToListAsync();

            return new ApiResponse 
            { 
                Success = true, 
                Data = new {
                    Items = contracts,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                }
            };
        }
        public async Task<ApiResponse> GetActiveContractByRoomAsync(int roomId)
        {
            var contract = await (from c in _db.Contracts
                                 join rs in _db.RoomSettings on c.RoomId equals rs.RoomId into rsGroup
                                 from rs in rsGroup.DefaultIfEmpty()
                                 where c.RoomId == roomId && c.ContractStatus == "Active"
                                 select new
                                 {
                                     c.ContractId,
                                     c.RoomId,
                                     c.PrimaryTenantId,
                                     PrimaryTenantName = c.PrimaryTenant.FullName,
                                     c.StartDate,
                                     c.EndDate,
                                     c.MonthlyRent,
                                     c.DepositAmount,
                                     c.Terms,
                                     StandardOccupants = rs != null ? rs.MaxOccupants : 2, // Lấy Max làm giá trị hiển thị ở UI
                                     ExtraOccupantFee = rs != null ? rs.ExtraOccupantFee : 0,
                                     SelectedServiceIds = _db.RoomServiceSettings
                                         .Where(rss => rss.RoomId == roomId && rss.IsActive)
                                         .Select(rss => rss.ServiceId)
                                         .ToList()
                                 }).FirstOrDefaultAsync();

            if (contract == null) return new ApiResponse { Success = false, Message = "Phòng chưa có hợp đồng hiệu lực." };
            return new ApiResponse { Success = true, Data = contract };
        }

        public async Task<ApiResponse> UpdateContractAsync(int contractId, ContractRequest request)
        {
            var contract = await _db.Contracts.FindAsync(contractId);
            if (contract == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hợp đồng." };

            contract.MonthlyRent = request.MonthlyRent;
            contract.DepositAmount = request.DepositAmount;
            contract.StartDate = request.StartDate;
            contract.EndDate = request.EndDate;
            contract.Terms = request.Terms;
            contract.UpdatedAt = DateTime.Now;

            // Đồng bộ sang RoomSetting
            var roomSetting = await _db.RoomSettings.FirstOrDefaultAsync(rs => rs.RoomId == contract.RoomId);
            if (roomSetting != null)
            {
                roomSetting.BaseRent = request.MonthlyRent;
                roomSetting.DepositAmount = request.DepositAmount;
                roomSetting.StandardOccupants = 2; // Ngưỡng phụ thu cố định là 2
                roomSetting.MaxOccupants = request.StandardOccupants; // Giới hạn tối đa nhập từ UI
                roomSetting.ExtraOccupantFee = request.ExtraOccupantFee;
                roomSetting.ApplyExtraOccupantFee = request.ExtraOccupantFee > 0;
                roomSetting.UpdatedAt = DateTime.Now;
            }

            // Đồng bộ Dịch vụ
            var globalServices = await _db.Services.Where(s => s.IsActive).ToListAsync();
            var roomServiceSettings = await _db.RoomServiceSettings.Where(rs => rs.RoomId == contract.RoomId).ToListAsync();
            
            foreach (var service in globalServices)
            {
                var setting = roomServiceSettings.FirstOrDefault(rs => rs.ServiceId == service.ServiceId);
                if (setting == null)
                {
                    setting = new RoomServiceSetting
                    {
                        RoomId = contract.RoomId,
                        ServiceId = service.ServiceId,
                        UnitPrice = service.DefaultPrice,
                        CalculationType = service.CalculationType,
                        CreatedAt = DateTime.Now
                    };
                    _db.RoomServiceSettings.Add(setting);
                }

                string sName = service.ServiceName?.ToLower() ?? "";
                bool isMandatory = sName.Contains("điện") || sName.Contains("nước");

                if (isMandatory)
                {
                    setting.IsActive = true;
                }
                else
                {
                    setting.IsActive = request.SelectedServiceIds != null && request.SelectedServiceIds.Contains(service.ServiceId);
                }
                setting.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật hợp đồng và cấu hình phòng thành công." };
        }
    }
}
