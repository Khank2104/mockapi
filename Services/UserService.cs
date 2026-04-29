using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
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
            Id = u.UserId.ToString(),
            Name = u.Name,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role?.RoleName ?? "tenant",
            Avatar = u.Avatar,
            Phone = u.Phone,
            Status = u.Status
        };

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
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

        public async Task<ApiResponse> UpdateAsync(int id, UpdateUserRequest request, int requesterId)
        {
            var requester = await GetByIdAsync(requesterId);
            var targetUser = await GetByIdAsync(id);

            if (targetUser == null || requester == null)
                return new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." };

            // Logic: User chỉ được sửa chính mình, Admin/Superuser được sửa target hợp lệ
            bool isSelf = requester.UserId == targetUser.UserId;
            bool isManager = requester.Role.RoleName == "admin" || requester.Role.RoleName == "superuser";

            if (!isSelf && !isManager)
                return new ApiResponse { Success = false, Message = "Bạn không có quyền sửa thông tin người khác." };

            if (isManager && targetUser.Role.RoleName == "superuser" && requester.UserId != targetUser.UserId)
                return new ApiResponse { Success = false, Message = "Không thể sửa thông tin Superuser khác." };

            targetUser.Name = request.Name;
            targetUser.Email = request.Email;
            targetUser.Avatar = request.Avatar ?? targetUser.Avatar;
            targetUser.Phone = request.Phone;
            targetUser.UpdatedAt = DateTime.Now;

            if (isManager && !string.IsNullOrEmpty(request.Status))
                targetUser.Status = request.Status;

            if (!string.IsNullOrEmpty(request.Password))
                targetUser.PasswordHash = _authService.HashPassword(request.Password);

            await _db.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(targetUser.UserId, "Cập nhật hồ sơ", "Thông tin tài khoản của bạn đã được cập nhật thành công.", "success");

            return new ApiResponse { Success = true, Message = "Cập nhật thành công." };
        }

        public async Task<ApiResponse> DeleteAsync(int id, int requesterId)
        {
            var requester = await GetByIdAsync(requesterId);
            var targetUser = await GetByIdAsync(id);

            if (targetUser == null || requester == null)
                return new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." };

            if (targetUser.Role.RoleName == "superuser")
                return new ApiResponse { Success = false, Message = "Tài khoản Superuser là bất tử." };

            if (requester.Role.RoleName != "superuser")
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền xóa tài khoản." };

            _db.Users.Remove(targetUser);
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Xóa người dùng thành công." };
        }

        public Task<ApiResponse> ToggleOtpAsync(int userId, bool otpEnabled)
        {
            return Task.FromResult(new ApiResponse { Success = false, Message = "Tính năng OTP không khả dụng trong phiên bản này." });
        }

        public async Task<ApiResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return new ApiResponse { Success = false, Message = "Người dùng không tồn tại." };

            if (!_authService.VerifyPassword(request.CurrentPassword, user))
                return new ApiResponse { Success = false, Message = "Mật khẩu hiện tại không chính xác." };

            user.PasswordHash = _authService.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            user.IsFirstLogin = false;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Đổi mật khẩu thành công." };
        }

        public async Task UpdatePasswordOnlyAsync(User user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
            await _db.SaveChangesAsync();
        }

    }
}
