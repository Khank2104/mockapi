using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthService _authService;

        public AdminService(ApplicationDbContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        private async Task<bool> IsSuperuser(int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Role?.RoleName == "superuser";
        }

        public async Task<ApiResponse> CreateAdminAsync(CreateAdminRequest request, int superuserId)
        {
            if (!await IsSuperuser(superuserId))
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền tạo Admin." };

            if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                return new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." };

            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "admin");
            if (adminRole == null) return new ApiResponse { Success = false, Message = "Role Admin không tồn tại." };

            var newAdmin = new User
            {
                Username = request.Username,
                PasswordHash = _authService.HashPassword(request.Password),
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                RoleId = adminRole.RoleId,
                Status = "Active",
                CreatedBy = superuserId,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(newAdmin);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo tài khoản Admin thành công." };
        }

        public async Task<ApiResponse> GetAllAdminsAsync(int superuserId)
        {
            if (!await IsSuperuser(superuserId))
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

        public async Task<ApiResponse> ToggleAdminStatusAsync(int adminId, bool active, int superuserId)
        {
            if (!await IsSuperuser(superuserId))
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
