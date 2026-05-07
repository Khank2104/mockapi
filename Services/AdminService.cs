using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthService _authService;
        private readonly IAccessControlService _accessControl;

        public AdminService(ApplicationDbContext db, IAuthService authService, IAccessControlService accessControl)
        {
            _db = db;
            _authService = authService;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> CreateAdminAsync(CreateAdminRequest request, int superuserId)
        {
            if (!await _accessControl.IsSuperuserAsync(superuserId))
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền tạo Admin." };

            if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                return new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." };

            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "admin");
            if (adminRole == null) return new ApiResponse { Success = false, Message = "Role Admin không tồn tại." };

            string password = string.IsNullOrWhiteSpace(request.Password) ? "123456" : request.Password;

            var newAdmin = new User
            {
                Username = request.Username,
                PasswordHash = _authService.HashPassword(password),
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                RoleId = adminRole.RoleId,
                Status = "Active",
                MustChangePassword = true,
                IsFirstLogin = true,
                CreatedBy = superuserId,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(newAdmin);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = $"Tạo tài khoản Admin thành công. Mật khẩu là: {password}" };
        }

        public async Task<ApiResponse> GetAllAdminsAsync(int superuserId)
        {
            if (!await _accessControl.IsSuperuserAsync(superuserId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var admins = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "admin")
                .Select(u => new UserResponse
                {
                    Id = u.UserId.ToString(),
                    Name = u.Name,
                    Username = u.Username,
                    Email = u.Email,
                    Role = "admin",
                    Phone = u.Phone,
                    Status = u.Status
                }).ToListAsync();

            return new ApiResponse { Success = true, Data = admins };
        }

        public async Task<ApiResponse> GetAdminByIdAsync(int adminId, int superuserId)
        {
            if (!await _accessControl.IsSuperuserAsync(superuserId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var admin = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.UserId == adminId && u.Role.RoleName == "admin")
                .Select(u => new 
                {
                    u.UserId,
                    u.Name,
                    u.Username,
                    u.Email,
                    u.Phone,
                    u.Status
                }).FirstOrDefaultAsync();

            if (admin == null) return new ApiResponse { Success = false, Message = "Không tìm thấy Admin." };
            return new ApiResponse { Success = true, Data = admin };
        }

        public async Task<ApiResponse> UpdateAdminAsync(int adminId, UpdateAdminRequest request, int superuserId)
        {
            if (!await _accessControl.IsSuperuserAsync(superuserId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var admin = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (admin == null || admin.Role.RoleName != "admin")
                return new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản Admin." };

            admin.Name = request.Name;
            admin.Email = request.Email;
            admin.Phone = request.Phone;
            
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                admin.PasswordHash = _authService.HashPassword(request.NewPassword);
                admin.MustChangePassword = true; // Buộc đổi lại nếu admin reset pass
            }

            admin.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Cập nhật tài khoản Admin thành công." };
        }

        public async Task<ApiResponse> DeleteAdminAsync(int adminId, int superuserId)
        {
            if (!await _accessControl.IsSuperuserAsync(superuserId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var admin = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (admin == null || admin.Role.RoleName != "admin")
                return new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản Admin." };

            // Kiểm tra xem Admin này có đang quản lý khu trọ nào không
            var ownsMotels = await _db.Motels.AnyAsync(m => m.OwnerUserId == adminId);
            if (ownsMotels)
                return new ApiResponse { Success = false, Message = "Không thể xóa Admin này vì đang quản lý một hoặc nhiều khu trọ. Vui lòng chuyển quyền quản lý trước." };

            _db.Users.Remove(admin);
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Đã xóa tài khoản Admin vĩnh viễn." };
        }

        public async Task<ApiResponse> ToggleAdminStatusAsync(int adminId, bool active, int superuserId)
        {
            if (!await _accessControl.IsSuperuserAsync(superuserId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var admin = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (admin == null || admin.Role.RoleName != "admin")
                return new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản Admin." };

            admin.Status = active ? "Active" : "Locked";
            admin.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            
            return new ApiResponse { Success = true, Message = $"Đã {(active ? "mở khóa" : "khóa")} tài khoản Admin." };
        }
    }
}
