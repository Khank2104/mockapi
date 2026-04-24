using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;

        public AuthService(IConfiguration config, ApplicationDbContext db)
        {
            _config = config;
            _db = db;
        }

        private string HashPasswordSha256(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string inputPassword, User userToVerify)
        {
            if (string.IsNullOrEmpty(inputPassword) || userToVerify == null || string.IsNullOrEmpty(userToVerify.Password)) return false;

            if (userToVerify.Password == inputPassword)
            {
                userToVerify.Password = HashPassword(inputPassword);
                _db.SaveChanges();
                return true;
            }

            if (userToVerify.Password.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(inputPassword, userToVerify.Password);
            }

            if (userToVerify.Password == HashPasswordSha256(inputPassword))
            {
                userToVerify.Password = HashPassword(inputPassword);
                _db.SaveChanges();
                return true;
            }

            return false;
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role),
                new Claim("id", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
