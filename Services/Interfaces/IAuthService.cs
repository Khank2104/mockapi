using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string inputPassword, User userToVerify);
        string GenerateJwtToken(User user);
    }
}
