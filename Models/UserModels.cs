using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    // Model đại diện cho dữ liệu lưu trong SQL Server
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? Phone { get; set; }
        public int RoleId { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public bool MustChangePassword { get; set; } = false;
        public bool IsFirstLogin { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Avatar { get; set; }

        [ForeignKey("RoleId")]
        
        public Role Role { get; set; } = null!;
    }


    // Model trả về Frontend (An toàn, không lộ mật khẩu)
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "tenant";
        public string? Avatar { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Cho User tự cập nhật profile
    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; } // Chỉ Admin/Superuser có thể update Status qua UserService.UpdateAsync
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    // --- Legacy Requests (Removed or moved to Phase2DTOs) ---
    // RegisterRequest - Removed
    // CreateUserRequest - Replaced by CreateAdminRequest / CreateTenantAccountRequest
}
