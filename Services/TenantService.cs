using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class TenantService : ITenantService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthService _authService;

        public TenantService(ApplicationDbContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        private async Task<bool> IsAdminOrSuper(int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Role?.RoleName == "admin" || user?.Role?.RoleName == "superuser";
        }

        public async Task<ApiResponse> CreateProfileAsync(CreateTenantProfileRequest request, int adminId)
        {
            if (!await IsAdminOrSuper(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var newTenant = new Tenant
            {
                FullName = request.FullName,
                CitizenId = request.CitizenId,
                Phone = request.Phone,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                PermanentAddress = request.PermanentAddress,
                EmergencyContact = request.EmergencyContact,
                TenantStatus = "Prospective",
                CreatedAt = DateTime.Now
            };

            _db.Tenants.Add(newTenant);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo hồ sơ khách thuê thành công.", Data = new { TenantId = newTenant.TenantId } };
        }

        public async Task<ApiResponse> UpdateProfileAsync(int tenantId, UpdateTenantProfileRequest request, int adminId)
        {
            if (!await IsAdminOrSuper(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var tenant = await _db.Tenants.FindAsync(tenantId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ khách thuê." };

            tenant.FullName = request.FullName;
            tenant.CitizenId = request.CitizenId;
            tenant.Phone = request.Phone;
            tenant.Gender = request.Gender;
            tenant.DateOfBirth = request.DateOfBirth;
            tenant.PermanentAddress = request.PermanentAddress;
            tenant.EmergencyContact = request.EmergencyContact;
            tenant.TenantStatus = request.TenantStatus;
            tenant.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật hồ sơ thành công." };
        }

        public async Task<ApiResponse> GetProfileByIdAsync(int tenantId)
        {
            var t = await _db.Tenants.Include(t => t.User).FirstOrDefaultAsync(t => t.TenantId == tenantId);
            if (t == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ." };

            var response = new TenantResponse
            {
                TenantId = t.TenantId,
                FullName = t.FullName,
                IdCard = t.CitizenId,
                Phone = t.Phone,
                Email = t.User?.Email,
                Status = t.TenantStatus
            };

            return new ApiResponse { Success = true, Data = response };
        }

        public async Task<ApiResponse> GetAllProfilesAsync(int adminId)
        {
            if (!await IsAdminOrSuper(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var tenants = await _db.Tenants
                .Include(t => t.User)
                .Include(t => t.RoomOccupancies).ThenInclude(ro => ro.Room)
                .Select(t => new TenantResponse
                {
                    TenantId = t.TenantId,
                    FullName = t.FullName,
                    IdCard = t.CitizenId,
                    Phone = t.Phone,
                    Email = t.User != null ? t.User.Email : "",
                    Status = t.TenantStatus,
                    CurrentRoomCode = t.RoomOccupancies
                        .Where(ro => ro.CheckOutDate == null)
                        .Select(ro => ro.Room.RoomCode)
                        .FirstOrDefault() ?? "N/A"
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = tenants };
        }

        public async Task<ApiResponse> CreateAccountAsync(CreateTenantAccountRequest request, int adminId)
        {
            if (!await IsAdminOrSuper(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var tenant = await _db.Tenants.FindAsync(request.TenantId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ khách thuê." };
            if (tenant.UserId != null) return new ApiResponse { Success = false, Message = "Khách thuê này đã có tài khoản." };

            if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                return new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." };

            var tenantRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "tenant");
            if (tenantRole == null) return new ApiResponse { Success = false, Message = "Role Tenant không tồn tại." };

            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = _authService.HashPassword(request.Password),
                Email = request.Email,
                Name = tenant.FullName,
                Phone = tenant.Phone,
                RoleId = tenantRole.RoleId,
                Status = "Active",
                MustChangePassword = true,
                IsFirstLogin = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            tenant.UserId = newUser.UserId;
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Cấp tài khoản cho khách thuê thành công." };
        }

        public async Task<ApiResponse> CreateTenantFullAsync(CreateTenantRequest request, int adminId)
        {
            if (!await IsAdminOrSuper(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Check if username exists
                if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                    return new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." };

                // 2. Get Role
                var tenantRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "tenant");
                if (tenantRole == null) return new ApiResponse { Success = false, Message = "Role Tenant không tồn tại." };

                // 3. Create User Account
                var newUser = new User
                {
                    Username = request.Username,
                    PasswordHash = _authService.HashPassword("123456"), // Default password
                    Email = request.Email,
                    Name = request.Name,
                    Phone = request.PhoneNumber,
                    RoleId = tenantRole.RoleId,
                    Status = "Active",
                    MustChangePassword = true,
                    IsFirstLogin = true,
                    CreatedBy = adminId,
                    CreatedAt = DateTime.Now
                };
                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();

                // 4. Create Tenant Profile
                var newTenant = new Tenant
                {
                    UserId = newUser.UserId,
                    FullName = request.Name,
                    Phone = request.PhoneNumber,
                    TenantStatus = "Staying",
                    CreatedAt = DateTime.Now
                };
                _db.Tenants.Add(newTenant);
                await _db.SaveChangesAsync();

                // 5. Create Contract
                var contract = new Contract
                {
                    RoomId = request.RoomId,
                    PrimaryTenantId = newTenant.TenantId,
                    MonthlyRent = request.MonthlyRent,
                    StartDate = DateTime.Now,
                    ContractStatus = "Active",
                    CreatedBy = adminId,
                    CreatedAt = DateTime.Now
                };
                _db.Contracts.Add(contract);

                // 6. Add to Room Occupants
                var occupant = new RoomOccupant
                {
                    RoomId = request.RoomId,
                    TenantId = newTenant.TenantId,
                    OccupantRole = "Primary",
                    CheckInDate = DateTime.Now
                };
                _db.RoomOccupants.Add(occupant);

                // 7. Update Room Status
                var room = await _db.Rooms.FindAsync(request.RoomId);
                if (room != null)
                {
                    room.Status = "Occupied";
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApiResponse { Success = true, Message = "Đã tạo tài khoản, hồ sơ và hợp đồng thuê phòng thành công." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse { Success = false, Message = "Lỗi hệ thống: " + ex.Message };
            }
        }
    }
}
