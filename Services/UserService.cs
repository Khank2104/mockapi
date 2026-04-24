using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Hubs;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthService _authService;
        private readonly INotificationService _notificationService;

        public UserService(ApplicationDbContext db, IAuthService authService, INotificationService notificationService)
        {
            _db = db;
            _authService = authService;
            _notificationService = notificationService;
        }

        private UserResponse ToResponse(User u) => new UserResponse
        {
            Id = u.Id.ToString(),
            Name = u.Name,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            Avatar = u.Avatar,
            PhoneNumber = u.PhoneNumber,
            Address = u.Address,
            Bio = u.Bio
        };

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _db.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _db.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User newUser)
        {
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            return newUser;
        }

        public async Task<ApiResponse> GetAllAsync(int requesterId)
        {
            var requester = await _db.Users.FindAsync(requesterId);
            if (requester == null || (requester.Role != "admin" && requester.Role != "superuser"))
                return new ApiResponse { Success = false, Message = "Bạn không có quyền truy cập danh sách người dùng." };

            var dbUsers = await _db.Users.ToListAsync();
            var usersResponse = dbUsers.Select(u => ToResponse(u)).ToList();
            return new ApiResponse { Success = true, Data = usersResponse };
        }

        public async Task<ApiResponse> CreateByAdminAsync(CreateUserRequest request, int requesterId)
        {
            var requester = await _db.Users.FindAsync(requesterId);
            if (requester == null || (requester.Role != "admin" && requester.Role != "superuser"))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            if (requester.Role == "admin" && request.Role != "user")
                return new ApiResponse { Success = false, Message = "Admin chỉ có quyền tạo tài khoản Role: User." };

            if (request.Role == "superuser")
                return new ApiResponse { Success = false, Message = "Không thể tạo thêm tài khoản Superuser." };

            bool exists = await UsernameExistsAsync(request.Username);
            if (exists)
                return new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." };

            var newUser = new User
            {
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                Password = _authService.HashPassword(request.Password),
                Role = request.Role,
                Avatar = request.Avatar ?? ""
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo người dùng thành công." };
        }

        public async Task<ApiResponse> UpdateAsync(int id, UpdateUserRequest request, int requesterId)
        {
            var requester = await _db.Users.FindAsync(requesterId);
            var targetUser = await _db.Users.FindAsync(id);

            if (targetUser == null || requester == null)
                return new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." };

            if (requester.Role == "user")
            {
                if (requester.Id != targetUser.Id) return new ApiResponse { Success = false, Message = "Bạn không thể sửa thông tin người khác." };
                if (request.Role != targetUser.Role) return new ApiResponse { Success = false, Message = "Bạn không được phép tự đổi Role." };
            }

            if (requester.Role == "admin")
            {
                if (targetUser.Role == "superuser") return new ApiResponse { Success = false, Message = "Admin không được chạm vào Superuser." };
                if (targetUser.Role == "admin" && requester.Id != targetUser.Id) return new ApiResponse { Success = false, Message = "Admin không được sửa Admin khác." };
                if (request.Role == "superuser") return new ApiResponse { Success = false, Message = "Không thể nâng cấp lên Superuser." };
                if (requester.Id == targetUser.Id && request.Role != targetUser.Role)
                    return new ApiResponse { Success = false, Message = "Admin không được tự thay đổi Role của chính mình." };
            }

            if (targetUser.Role == "superuser" && request.Role != "superuser")
                return new ApiResponse { Success = false, Message = "Tài khoản Superuser là cố định." };

            targetUser.Name = request.Name;
            targetUser.Username = request.Username;
            targetUser.Email = request.Email;
            targetUser.Role = request.Role;
            targetUser.Avatar = request.Avatar ?? targetUser.Avatar;
            targetUser.PhoneNumber = request.PhoneNumber;
            targetUser.Address = request.Address;
            targetUser.Bio = request.Bio;

            if (!string.IsNullOrEmpty(request.Password))
                targetUser.Password = _authService.HashPassword(request.Password);

            await _db.SaveChangesAsync();

            // Gửi thông báo thông minh
            if (requester.Id == targetUser.Id)
            {
                // User tự cập nhật
                await _notificationService.CreateNotificationAsync(targetUser.Id, "Cập nhật hồ sơ", "Thông tin cá nhân của bạn đã được cập nhật thành công.", "success");
            }
            else
            {
                // Admin cập nhật cho User
                await _notificationService.CreateNotificationAsync(targetUser.Id, "Hồ sơ đã thay đổi", "Thông tin cá nhân của bạn đã được cập nhật bởi Quản trị viên.", "info");
                await _notificationService.CreateNotificationForRolesAsync(new[] { "admin", "superuser" }, "Quản lý User", $"Đã cập nhật thông tin cho tài khoản {targetUser.Username}.", "info");
            }

            return new ApiResponse { Success = true, Message = "Cập nhật thành công." };
        }

        public async Task<ApiResponse> DeleteAsync(int id, int requesterId)
        {
            var requester = await _db.Users.FindAsync(requesterId);
            var targetUser = await _db.Users.FindAsync(id);

            if (targetUser == null || requester == null)
                return new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." };

            if (targetUser.Role == "superuser")
                return new ApiResponse { Success = false, Message = "Tài khoản Superuser là bất tử." };

            if (requester.Role == "user")
                return new ApiResponse { Success = false, Message = "Bạn không có quyền xóa." };

            if (requester.Role == "admin" && targetUser.Role != "user")
                return new ApiResponse { Success = false, Message = "Admin chỉ có quyền xóa User thường." };

            if (requester.Id == id)
                return new ApiResponse { Success = false, Message = "Không thể tự xóa chính mình." };

            _db.Users.Remove(targetUser);
            await _db.SaveChangesAsync();

            // Thông báo cho các quản trị viên khác
            await _notificationService.CreateNotificationForRolesAsync(new[] { "admin", "superuser" }, "Xóa người dùng", $"Tài khoản {targetUser.Username} đã bị xóa khỏi hệ thống bởi {requester.Username}.", "warning");

            return new ApiResponse { Success = true, Message = "Xóa người dùng thành công." };
        }

        public async Task<ApiResponse> ToggleOtpAsync(int userId, bool otpEnabled)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản." };

            user.OtpEnabled = otpEnabled;
            await _db.SaveChangesAsync();
            string status = otpEnabled ? "bật" : "tắt";
            return new ApiResponse { Success = true, Message = $"Đã {status} xác thực OTP thành công." };
        }

        public async Task UpdatePasswordOnlyAsync(User user, string hashedPassword)
        {
            user.Password = hashedPassword;
            await _db.SaveChangesAsync();
        }
    }
}
