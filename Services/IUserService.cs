using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IUserService
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<User> CreateUserAsync(User newUser);
        Task<ApiResponse> UpdateAsync(int id, UpdateUserRequest request, int requesterId);
        Task<ApiResponse> DeleteAsync(int id, int requesterId);
        Task<ApiResponse> ToggleOtpAsync(int userId, bool otpEnabled);
        Task UpdatePasswordOnlyAsync(User user, string hashedPassword);
    }
}
