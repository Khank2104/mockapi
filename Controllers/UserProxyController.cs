using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using UserManagementSystem.Models;
using UserManagementSystem.Services;
using UserManagementSystem.Hubs;
using UserManagementSystem.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProxyController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _config;

        public UserProxyController(ApplicationDbContext db, IEmailService emailService, IMemoryCache cache, IHubContext<NotificationHub> hubContext, IConfiguration config)
        {
            _db = db;
            _emailService = emailService;
            _cache = cache;
            _hubContext = hubContext;
            _config = config;
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

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string inputPassword, User userToVerify)
        {
            if (string.IsNullOrEmpty(inputPassword) || userToVerify == null || string.IsNullOrEmpty(userToVerify.Password)) return false;

            // Kiểm tra nếu là Plaintext (dữ liệu cũ từ MockAPI)
            if (userToVerify.Password == inputPassword)
            {
                userToVerify.Password = HashPassword(inputPassword);
                _db.SaveChanges(); // Auto-upgrade lên BCrypt
                return true;
            }

            // Kiểm tra nếu đã là BCrypt (bắt đầu bằng $2)
            if (userToVerify.Password.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(inputPassword, userToVerify.Password);
            }

            // Fallback: Kiểm tra SHA-256
            if (userToVerify.Password == HashPasswordSha256(inputPassword))
            {
                userToVerify.Password = HashPassword(inputPassword);
                _db.SaveChanges(); // Auto-upgrade lên BCrypt
                return true;
            }

            return false;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role), // Lưu Role để authorize
                new Claim("id", user.Id.ToString()) // Lưu Id rõ ràng
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6), // Thời hạn 6 tiếng
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddHours(6),
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("X-Access-Token", token, cookieOptions);
        }

        private UserResponse ToResponse(User u) => new UserResponse
        {
            Id = u.Id.ToString(),
            Name = u.Name,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            Avatar = u.Avatar
        };

        private int? GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (int.TryParse(idClaim, out int reqId)) return reqId;
            return null;
        }

        // ────────────────── GET ALL ──────────────────
        [Authorize]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            int? reqId = GetRequesterId();
            if (reqId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var requester = await _db.Users.FindAsync(reqId.Value);
            if (requester == null || (requester.Role != "admin" && requester.Role != "superuser"))
                return StatusCode(403, new ApiResponse { Success = false, Message = "Bạn không có quyền truy cập danh sách người dùng." });

            var dbUsers = await _db.Users.ToListAsync();
            var usersResponse = dbUsers.Select(u => ToResponse(u)).ToList();
            return Ok(usersResponse);
        }

        // ────────────────── LOGIN ──────────────────
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new ApiResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user != null && VerifyPassword(request.Password, user))
            {
                // Nếu user TẮT OTP → đăng nhập thẳng không cần xác thực
                if (!user.OtpEnabled)
                {
                    string token = GenerateJwtToken(user);
                    SetTokenCookie(token);
                    return Ok(new ApiResponse { Success = true, Data = new { User = ToResponse(user) } });
                }

                // Nếu user BẬT OTP → gửi mã và yêu cầu xác thực
                string otp = new Random().Next(100000, 999999).ToString();
                _cache.Set($"OTP_{user.Username}", otp, TimeSpan.FromMinutes(5));
                await _emailService.SendEmailAsync(user.Email, "Mã xác thực OTP Đăng nhập",
                    $"<h3>Chào {user.Name},</h3><p>Mã OTP của bạn là: <b style='font-size:24px'>{otp}</b></p><p>Mã có hiệu lực trong 5 phút.</p>");

                return Ok(new ApiResponse { Success = true, Message = "OTP_REQUIRED", Data = user.Username });
            }

            return Unauthorized(new ApiResponse { Success = false, Message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        // ────────────────── VERIFY OTP ──────────────────
        [HttpPost("VerifyOTP")]
        public async Task<IActionResult> VerifyOTP([FromBody] JsonElement body)
        {
            string username = body.GetProperty("username").GetString() ?? "";
            string otp = body.GetProperty("otp").GetString() ?? "";

            if (_cache.TryGetValue($"OTP_{username}", out string? savedOtp) && savedOtp == otp)
            {
                _cache.Remove($"OTP_{username}");
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    string token = GenerateJwtToken(user);
                    SetTokenCookie(token);
                    return Ok(new ApiResponse { Success = true, Data = new { User = ToResponse(user) } });
                }
            }

            return BadRequest(new ApiResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." });
        }

        // ────────────────── LOGOUT ──────────────────
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("X-Access-Token");
            return Ok(new ApiResponse { Success = true, Message = "Đã đăng xuất." });
        }

        // ────────────────── REGISTER - BƯỚC 1: Gửi OTP ──────────────────
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password)
                || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name))
                return BadRequest(new ApiResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });

            // Kiểm tra trùng username hoặc email ngay từ đầu
            bool usernameExists = await _db.Users.AnyAsync(u => u.Username == request.Username);
            if (usernameExists)
                return BadRequest(new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." });

            bool emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest(new ApiResponse { Success = false, Message = "Email này đã được sử dụng." });

            // Lưu thông tin đăng ký tạm thời vào Cache (chờ xác thực OTP)
            string otp = new Random().Next(100000, 999999).ToString();
            _cache.Set($"REGISTER_OTP_{request.Email}", otp, TimeSpan.FromMinutes(10));
            _cache.Set($"REGISTER_DATA_{request.Email}", request, TimeSpan.FromMinutes(10));

            // Gửi OTP qua email
            await _emailService.SendEmailAsync(request.Email, "Xác thực đăng ký tài khoản",
                $"<h3>Chào {request.Name},</h3>" +
                $"<p>Cảm ơn bạn đã đăng ký tài khoản!</p>" +
                $"<p>Mã OTP xác thực của bạn là: <b style='font-size:28px;color:#4f46e5'>{otp}</b></p>" +
                $"<p>Mã có hiệu lực trong <b>10 phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>");

            return Ok(new ApiResponse { Success = true, Message = "OTP_REQUIRED", Data = request.Email });
        }

        // ────────────────── REGISTER - BƯỚC 2: Xác thực OTP → Lưu DB ──────────────────
        [HttpPost("VerifyRegisterOTP")]
        public async Task<IActionResult> VerifyRegisterOTP([FromBody] JsonElement body)
        {
            string email = body.GetProperty("email").GetString() ?? "";
            string otp = body.GetProperty("otp").GetString() ?? "";

            // Kiểm tra OTP
            if (!_cache.TryGetValue($"REGISTER_OTP_{email}", out string? savedOtp) || savedOtp != otp)
                return BadRequest(new ApiResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." });

            // Lấy thông tin đăng ký đã lưu tạm
            if (!_cache.TryGetValue($"REGISTER_DATA_{email}", out RegisterRequest? registerData) || registerData == null)
                return BadRequest(new ApiResponse { Success = false, Message = "Phiên đăng ký đã hết hạn. Vui lòng thử lại." });

            // Kiểm tra lại lần nữa phòng trường hợp đăng ký trùng trong lúc chờ OTP
            bool usernameExists = await _db.Users.AnyAsync(u => u.Username == registerData.Username);
            if (usernameExists)
                return BadRequest(new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." });

            // Xác thực thành công → Lưu vào Database
            var newUser = new User
            {
                Name = registerData.Name,
                Username = registerData.Username,
                Email = registerData.Email,
                Password = HashPassword(registerData.Password),
                Role = "user",
                Avatar = ""
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // Xóa cache sau khi đã lưu thành công
            _cache.Remove($"REGISTER_OTP_{email}");
            _cache.Remove($"REGISTER_DATA_{email}");

            // Thông báo real-time cho admin
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Thành viên mới",
                $"Người dùng {registerData.Name} vừa đăng ký tài khoản thành công.", "success");

            return Ok(new ApiResponse { Success = true, Message = "Đăng ký tài khoản thành công! Vui lòng đăng nhập." });
        }


        // ────────────────── CREATE BY ADMIN ──────────────────
        [Authorize]
        [HttpPost("CreateByAdmin")]
        public async Task<IActionResult> CreateByAdmin([FromBody] CreateUserRequest request)
        {
            int? reqId = GetRequesterId();
            if (reqId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var requester = await _db.Users.FindAsync(reqId.Value);
            if (requester == null || (requester.Role != "admin" && requester.Role != "superuser"))
                return StatusCode(403, new ApiResponse { Success = false, Message = "Quyền hạn không đủ." });

            if (requester.Role == "admin" && request.Role != "user")
                return BadRequest(new ApiResponse { Success = false, Message = "Admin chỉ có quyền tạo tài khoản Role: User." });

            if (request.Role == "superuser")
                return BadRequest(new ApiResponse { Success = false, Message = "Không thể tạo thêm tài khoản Superuser." });

            bool exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
            if (exists)
                return BadRequest(new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." });

            var newUser = new User
            {
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                Password = HashPassword(request.Password),
                Role = request.Role,
                Avatar = request.Avatar ?? ""
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            return Ok(new ApiResponse { Success = true, Message = "Tạo người dùng thành công." });
        }

        // ────────────────── UPDATE ──────────────────
        [Authorize]
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            int? reqId = GetRequesterId();
            if (reqId == null)
                return StatusCode(401, new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var requester = await _db.Users.FindAsync(reqId.Value);
            var targetUser = await _db.Users.FindAsync(id);

            if (targetUser == null || requester == null)
                return NotFound(new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." });

            if (requester.Role == "user")
            {
                if (requester.Id != targetUser.Id) return StatusCode(403, new ApiResponse { Success = false, Message = "Bạn không thể sửa thông tin người khác." });
                if (request.Role != targetUser.Role) return BadRequest(new ApiResponse { Success = false, Message = "Bạn không được phép tự đổi Role." });
            }

            if (requester.Role == "admin")
            {
                if (targetUser.Role == "superuser") return StatusCode(403, new ApiResponse { Success = false, Message = "Admin không được chạm vào Superuser." });
                if (targetUser.Role == "admin" && requester.Id != targetUser.Id) return StatusCode(403, new ApiResponse { Success = false, Message = "Admin không được sửa Admin khác." });
                if (request.Role == "superuser") return BadRequest(new ApiResponse { Success = false, Message = "Không thể nâng cấp lên Superuser." });
                if (requester.Id == targetUser.Id && request.Role != targetUser.Role)
                    return BadRequest(new ApiResponse { Success = false, Message = "Admin không được tự thay đổi Role của chính mình." });
            }

            if (targetUser.Role == "superuser" && request.Role != "superuser")
                return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản Superuser là cố định." });

            targetUser.Name = request.Name;
            targetUser.Username = request.Username;
            targetUser.Email = request.Email;
            targetUser.Role = request.Role;
            targetUser.Avatar = request.Avatar ?? targetUser.Avatar;
            if (!string.IsNullOrEmpty(request.Password))
                targetUser.Password = HashPassword(request.Password);

            await _db.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Cập nhật tài khoản", $"Tài khoản {targetUser.Username} vừa được cập nhật thông tin.", "info");
            return Ok(new ApiResponse { Success = true, Message = "Cập nhật thành công." });
        }

        // ────────────────── DELETE ──────────────────
        [Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int? reqId = GetRequesterId();
            if (reqId == null)
                return StatusCode(401, new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var requester = await _db.Users.FindAsync(reqId.Value);
            var targetUser = await _db.Users.FindAsync(id);

            if (targetUser == null || requester == null)
                return NotFound(new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." });

            if (targetUser.Role == "superuser")
                return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản Superuser là bất tử." });

            if (requester.Role == "user")
                return StatusCode(403, new ApiResponse { Success = false, Message = "Bạn không có quyền xóa." });

            if (requester.Role == "admin" && targetUser.Role != "user")
                return StatusCode(403, new ApiResponse { Success = false, Message = "Admin chỉ có quyền xóa User thường." });

            if (requester.Id == id)
                return BadRequest(new ApiResponse { Success = false, Message = "Không thể tự xóa chính mình." });

            _db.Users.Remove(targetUser);
            await _db.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Xóa tài khoản", $"Tài khoản {targetUser.Username} đã bị xóa khỏi hệ thống.", "warning");
            return Ok(new ApiResponse { Success = true, Message = "Xóa người dùng thành công." });
        }

        // ────────────────── GET SETTINGS ──────────────────
        [Authorize]
        [HttpGet("GetSettings")]
        public async Task<IActionResult> GetSettings()
        {
            int? reqId = GetRequesterId();
            if (reqId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var user = await _db.Users.FindAsync(reqId.Value);
            if (user == null)
                return NotFound(new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản." });

            return Ok(new ApiResponse
            {
                Success = true,
                Data = new { otpEnabled = user.OtpEnabled }
            });
        }

        // ────────────────── TOGGLE OTP ──────────────────
        [Authorize]
        [HttpPost("ToggleOTP")]
        public async Task<IActionResult> ToggleOTP([FromBody] JsonElement body)
        {
            int? reqId = GetRequesterId();
            if (reqId == null)
                return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var user = await _db.Users.FindAsync(reqId.Value);
            if (user == null)
                return NotFound(new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản." });

            bool otpEnabled = body.GetProperty("otpEnabled").GetBoolean();
            user.OtpEnabled = otpEnabled;
            await _db.SaveChangesAsync();

            string status = otpEnabled ? "bật" : "tắt";
            return Ok(new ApiResponse { Success = true, Message = $"Đã {status} xác thực OTP thành công." });
        }
    }
}
