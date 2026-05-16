using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class ContractService : IContractService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAccessControlService _accessControl;

        public ContractService(ApplicationDbContext db, IAccessControlService accessControl)
        {
            _db = db;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> CreateContractAsync(ContractRequest request, int adminId)
        {
            var room = await _db.Rooms
                .Include(r => r.Motel)
                .Include(r => r.Floor)
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId);

            if (room == null || !await _accessControl.IsAdminOfMotelAsync(adminId, room.MotelId))
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
            
            roomSetting.StandardOccupants = 2; 
            roomSetting.MaxOccupants = request.StandardOccupants; 
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
                    setting.IsActive = true;
                }
                else
                {
                    setting.IsActive = request.SelectedServiceIds != null && request.SelectedServiceIds.Contains(service.ServiceId);
                }
                setting.UpdatedAt = DateTime.Now;
            }

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

            if (contract == null || !await _accessControl.IsAdminOfMotelAsync(adminId, contract.Room.MotelId))
                return new ApiResponse { Success = false, Message = "Không tìm thấy hợp đồng hoặc quyền hạn không đủ." };

            int roomId = contract.RoomId;
            contract.ContractStatus = "Terminated";
            contract.EndDate = DateTime.Now;
            contract.UpdatedAt = DateTime.Now;

            var occupants = await _db.RoomOccupants.Where(o => o.RoomId == roomId && o.Status == "Staying").ToListAsync();
            foreach (var occupant in occupants)
            {
                occupant.Status = "MovedOut";
                occupant.CheckOutDate = DateTime.Now;
                occupant.UpdatedAt = DateTime.Now;
            }

            var tenantIds = occupants.Select(o => o.TenantId).ToList();
            var tenants = await _db.Tenants.Where(t => tenantIds.Contains(t.TenantId)).ToListAsync();
            foreach (var tenant in tenants)
            {
                tenant.TenantStatus = "MovedOut";
                if (tenant.UserId.HasValue)
                {
                    var user = await _db.Users.FindAsync(tenant.UserId.Value);
                    if (user != null) _db.Users.Remove(user);
                    tenant.UserId = null;
                }
                tenant.UpdatedAt = DateTime.Now;
            }

            var room = await _db.Rooms.FindAsync(roomId);
            if (room != null)
            {
                room.Status = "Vacant";
                room.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Đã thanh lý hợp đồng thành công." };
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
                    items = contracts,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    currentPage = page,
                    pageSize = pageSize
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
                                     contractId = c.ContractId,
                                     roomId = c.RoomId,
                                     primaryTenantId = c.PrimaryTenantId,
                                     primaryTenantName = c.PrimaryTenant.FullName,
                                     startDate = c.StartDate,
                                     endDate = c.EndDate,
                                     monthlyRent = c.MonthlyRent,
                                     depositAmount = c.DepositAmount,
                                     terms = c.Terms,
                                     standardOccupants = rs != null ? rs.MaxOccupants : 2, 
                                     extraOccupantFee = rs != null ? rs.ExtraOccupantFee : 0,
                                     selectedServiceIds = _db.RoomServiceSettings
                                         .Where(rss => rss.RoomId == roomId && rss.IsActive)
                                         .Select(rss => rss.ServiceId)
                                         .ToList()
                                 }).FirstOrDefaultAsync();

            if (contract == null) return new ApiResponse { Success = false, Message = "Phòng chưa có hợp đồng hiệu lực." };
            return new ApiResponse { Success = true, Data = contract };
        }

        public async Task<ApiResponse> GetContractForPrintAsync(int contractId, int adminId)
        {
            var contract = await _db.Contracts
                .Include(c => c.Room).ThenInclude(r => r.Motel).ThenInclude(m => m.Owner)
                .Include(c => c.PrimaryTenant)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null || !await _accessControl.IsAdminOfMotelAsync(adminId, contract.Room.MotelId))
                return new ApiResponse { Success = false, Message = "Không tìm thấy hợp đồng hoặc bạn không có quyền." };

            var owner = contract.Room.Motel.Owner;
            var tenant = contract.PrimaryTenant;

            var printData = new
            {
                contractId = contract.ContractId,
                startDate = contract.StartDate,
                endDate = contract.EndDate,
                monthlyRent = contract.MonthlyRent,
                depositAmount = contract.DepositAmount,
                roomCode = contract.Room.RoomCode,
                motelName = contract.Room.Motel.MotelName,
                motelAddress = contract.Room.Motel.Address,
                ownerName = owner?.Name ?? "Chủ trọ",
                ownerPhone = owner?.Phone ?? "",
                tenantName = tenant?.FullName ?? "",
                tenantPhone = tenant?.Phone ?? "",
                tenantIdentityCard = tenant?.CitizenId ?? "",
                tenantAddress = tenant?.PermanentAddress ?? "",
                terms = contract.Terms ?? ""
            };

            return new ApiResponse { Success = true, Data = printData };
        }

        public async Task<ApiResponse> UpdateContractAsync(int contractId, ContractRequest request, int adminId)
        {
            var contract = await _db.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Motel)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null || !await _accessControl.IsAdminOfMotelAsync(adminId, contract.Room.MotelId))
                return new ApiResponse { Success = false, Message = "Không tìm thấy hợp đồng hoặc bạn không có quyền." };

            contract.MonthlyRent = request.MonthlyRent;
            contract.DepositAmount = request.DepositAmount;
            contract.StartDate = request.StartDate;
            contract.EndDate = request.EndDate;
            contract.Terms = request.Terms;
            contract.UpdatedAt = DateTime.Now;

            var roomSetting = await _db.RoomSettings.FirstOrDefaultAsync(rs => rs.RoomId == contract.RoomId);
            if (roomSetting != null)
            {
                roomSetting.BaseRent = request.MonthlyRent;
                roomSetting.DepositAmount = request.DepositAmount;
                roomSetting.StandardOccupants = 2;
                roomSetting.MaxOccupants = request.StandardOccupants;
                roomSetting.ExtraOccupantFee = request.ExtraOccupantFee;
                roomSetting.ApplyExtraOccupantFee = request.ExtraOccupantFee > 0;
                roomSetting.UpdatedAt = DateTime.Now;
            }

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

                if (isMandatory) setting.IsActive = true;
                else setting.IsActive = request.SelectedServiceIds != null && request.SelectedServiceIds.Contains(service.ServiceId);
                
                setting.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật hợp đồng thành công." };
        }
    }
}
