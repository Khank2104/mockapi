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
            var tenant = await _db.Tenants.Include(t => t.User).FirstOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ." };
            return new ApiResponse { Success = true, Data = tenant };
        }

        public async Task<ApiResponse> GetAllProfilesAsync(int adminId)
        {
            if (!await IsAdminOrSuper(adminId)) return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };
            var tenants = await _db.Tenants.Include(t => t.User).ToListAsync();
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
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            tenant.UserId = newUser.UserId;
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Cấp tài khoản cho khách thuê thành công." };
        }
    }
}
