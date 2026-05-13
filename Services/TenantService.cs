using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class TenantService : ITenantService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthService _authService;
        private readonly IAccessControlService _accessControl;

        public TenantService(ApplicationDbContext db, IAuthService authService, IAccessControlService accessControl)
        {
            _db = db;
            _authService = authService;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> CreateProfileAsync(CreateTenantProfileRequest request, int adminId)
        {
            if (!await _accessControl.IsAdminOrSuperAsync(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

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
            if (!await _accessControl.IsAdminOrSuperAsync(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

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

        public async Task<ApiResponse> GetAllProfilesAsync(int adminId, string? searchTerm = null, int page = 1, int pageSize = 10, int? motelId = null)
        {
            if (!await _accessControl.IsAdminOrSuperAsync(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var query = _db.Tenants
                .Include(t => t.User)
                .Include(t => t.RoomOccupancies).ThenInclude(ro => ro.Room)
                .AsQueryable();

            if (motelId.HasValue)
            {
                query = query.Where(t => t.RoomOccupancies.Any(ro => ro.Room.MotelId == motelId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(t => 
                    (t.FullName != null && t.FullName.ToLower().Contains(searchTerm)) || 
                    (t.Phone != null && t.Phone.Contains(searchTerm)) || 
                    (t.CitizenId != null && t.CitizenId.Contains(searchTerm)) ||
                    (t.User != null && t.User.Username.ToLower().Contains(searchTerm))
                );
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var tenants = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TenantResponse
                {
                    TenantId = t.TenantId,
                    FullName = t.FullName,
                    IdCard = t.CitizenId,
                    Phone = t.Phone,
                    Email = t.User != null ? t.User.Email : "",
                    Status = t.RoomOccupancies.Any(ro => ro.Status == "Staying") ? "Staying" : "Active",
                    CurrentRoomCode = t.RoomOccupancies
                        .Where(ro => ro.Status == "Staying")
                        .Select(ro => ro.Room.RoomCode)
                        .FirstOrDefault() ?? "N/A",
                    HasActiveContract = _db.Contracts.Any(c => c.PrimaryTenantId == t.TenantId && c.ContractStatus == "Active")
                })
                .ToListAsync();

            return new ApiResponse 
            { 
                Success = true, 
                Data = new {
                    items = tenants,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    currentPage = page,
                    pageSize = pageSize
                }
            };
        }

        public async Task<ApiResponse> CreateAccountAsync(CreateTenantAccountRequest request, int adminId)
        {
            if (!await _accessControl.IsAdminOrSuperAsync(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

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
            if (!await _accessControl.IsAdminOrSuperAsync(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

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
                    PasswordHash = _authService.HashPassword(request.Password),
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
                    TenantStatus = "Prospective", // Mới tạo chưa vào ở nên để Prospective
                    CreatedAt = DateTime.Now
                };
                _db.Tenants.Add(newTenant);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ApiResponse { Success = true, Message = "Đã tạo tài khoản và hồ sơ khách thuê thành công." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse { Success = false, Message = "Lỗi hệ thống: " + ex.Message };
            }
        }

        public async Task<ApiResponse> GetProfileByUserIdAsync(int userId)
        {
            var t = await _db.Tenants.Include(t => t.User).FirstOrDefaultAsync(t => t.UserId == userId);
            if (t == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ khách thuê." };

            return new ApiResponse { 
                Success = true, 
                Data = new {
                    t.TenantId,
                    t.FullName,
                    t.CitizenId,
                    t.Phone,
                    t.Gender,
                    t.DateOfBirth,
                    t.PermanentAddress,
                    t.EmergencyContact,
                    Email = t.User?.Email
                } 
            };
        }

        public async Task<ApiResponse> UpdateProfileByUserIdAsync(int userId, UpdateTenantProfileRequest request)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ khách thuê." };

            tenant.FullName = request.FullName;
            tenant.CitizenId = request.CitizenId;
            tenant.Phone = request.Phone;
            tenant.Gender = request.Gender;
            tenant.DateOfBirth = request.DateOfBirth;
            tenant.PermanentAddress = request.PermanentAddress;
            tenant.EmergencyContact = request.EmergencyContact;
            tenant.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Cập nhật thông tin hồ sơ cá nhân thành công." };
        }
    }
}
