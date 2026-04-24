using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using UserManagementSystem.Models;
using UserManagementSystem.Services;
using UserManagementSystem.Hubs;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProxyController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        public UserProxyController(IEmailService emailService, IAuthService authService, IOtpService otpService, IUserService userService, INotificationService notificationService, IWebHostEnvironment env)
        {
            _emailService = emailService;
            _authService = authService;
            _otpService = otpService;
            _userService = userService;
            _notificationService = notificationService;
            _env = env;
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
            Avatar = u.Avatar,
            PhoneNumber = u.PhoneNumber,
            Address = u.Address,
            Bio = u.Bio
        };

        private int? GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (int.TryParse(idClaim, out int reqId)) return reqId;
            return null;
        }

        // ────────────────── GET ALL ──────────────────
        [Authorize]
        [HttpGet("GetMyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var reqId = GetRequesterId();
            if (reqId == null) return Unauthorized();

            var user = await _userService.GetByIdAsync(reqId.Value);
            if (user == null) return NotFound();

            return Ok(new { success = true, data = ToResponse(user) });
        }

        [Authorize]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var result = await _userService.GetAllAsync(reqId.Value);
            return result.Success ? Ok(result.Data) : StatusCode(403, result);
        }

        // ────────────────── LOGIN ──────────────────
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new ApiResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });

            var user = await _userService.GetByUsernameAsync(request.Username);

            if (user != null && _authService.VerifyPassword(request.Password, user))
            {
                if (!user.OtpEnabled)
                {
                    string token = _authService.GenerateJwtToken(user);
                    SetTokenCookie(token);
                    return Ok(new ApiResponse { Success = true, Data = new { User = ToResponse(user) } });
                }

                string otp = _otpService.GenerateAndSaveOtp($"OTP_{user.Username}", TimeSpan.FromMinutes(5));
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

            if (_otpService.VerifyAndRemoveOtp($"OTP_{username}", otp))
            {
                var user = await _userService.GetByUsernameAsync(username);
                if (user != null)
                {
                    string token = _authService.GenerateJwtToken(user);
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

        // ────────────────── REGISTER ──────────────────
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password)
                || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Name))
                return BadRequest(new ApiResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });

            if (await _userService.UsernameExistsAsync(request.Username))
                return BadRequest(new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." });

            if (await _userService.EmailExistsAsync(request.Email))
                return BadRequest(new ApiResponse { Success = false, Message = "Email này đã được sử dụng." });

            string otp = _otpService.GenerateAndSaveOtp($"REGISTER_OTP_{request.Email}", TimeSpan.FromMinutes(10));
            _otpService.SaveDataToCache($"REGISTER_DATA_{request.Email}", request, TimeSpan.FromMinutes(10));

            await _emailService.SendEmailAsync(request.Email, "Xác thực đăng ký tài khoản",
                $"<h3>Chào {request.Name},</h3><p>Cảm ơn bạn đã đăng ký tài khoản!</p><p>Mã OTP xác thực của bạn là: <b style='font-size:28px;color:#4f46e5'>{otp}</b></p><p>Mã có hiệu lực trong <b>10 phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>");

            return Ok(new ApiResponse { Success = true, Message = "OTP_REQUIRED", Data = request.Email });
        }

        // ────────────────── VERIFY REGISTER OTP ──────────────────
        [HttpPost("VerifyRegisterOTP")]
        public async Task<IActionResult> VerifyRegisterOTP([FromBody] JsonElement body)
        {
            string email = body.GetProperty("email").GetString() ?? "";
            string otp = body.GetProperty("otp").GetString() ?? "";

            var registerData = _otpService.GetDataFromCache<RegisterRequest>($"REGISTER_DATA_{email}");
            if (registerData == null) return BadRequest(new ApiResponse { Success = false, Message = "Phiên đăng ký đã hết hạn. Vui lòng thử lại." });

            if (!_otpService.VerifyAndRemoveOtp($"REGISTER_OTP_{email}", otp))
                return BadRequest(new ApiResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." });

            if (await _userService.UsernameExistsAsync(registerData.Username))
                return BadRequest(new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." });

            var newUser = new User
            {
                Name = registerData.Name,
                Username = registerData.Username,
                Email = registerData.Email,
                Password = _authService.HashPassword(registerData.Password),
                Role = "user",
                Avatar = ""
            };

            await _userService.CreateUserAsync(newUser);
            _otpService.RemoveCache($"REGISTER_DATA_{email}");

            // Thông báo cho Admin
            await _notificationService.CreateNotificationForRolesAsync(
                new[] { "admin", "superuser" }, 
                "Thành viên mới", 
                $"Người dùng {registerData.Name} vừa đăng ký tài khoản thành công.", 
                "success"
            );

            // Thông báo chào mừng cho chính User mới
            var createdUser = await _userService.GetByUsernameAsync(newUser.Username);
            if (createdUser != null)
            {
                await _notificationService.CreateNotificationAsync(
                    createdUser.Id,
                    "Chào mừng bạn!",
                    "Chúc mừng bạn đã gia nhập hệ thống. Hãy hoàn thiện hồ sơ của mình nhé!",
                    "success"
                );
            }
            
            return Ok(new ApiResponse { Success = true, Message = "Đăng ký tài khoản thành công! Vui lòng đăng nhập." });
        }

        // ────────────────── CREATE BY ADMIN ──────────────────
        [Authorize]
        [HttpPost("CreateByAdmin")]
        public async Task<IActionResult> CreateByAdmin([FromBody] CreateUserRequest request)
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var result = await _userService.CreateByAdminAsync(request, reqId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ────────────────── UPDATE ──────────────────
        [Authorize]
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return StatusCode(401, new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var result = await _userService.UpdateAsync(id, request, reqId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ────────────────── DELETE ──────────────────
        [Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return StatusCode(401, new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var result = await _userService.DeleteAsync(id, reqId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ────────────────── GET SETTINGS ──────────────────
        [Authorize]
        [HttpGet("GetSettings")]
        public async Task<IActionResult> GetSettings()
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var user = await _userService.GetByIdAsync(reqId.Value);
            if (user == null) return NotFound(new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản." });

            return Ok(new ApiResponse { Success = true, Data = new { otpEnabled = user.OtpEnabled } });
        }

        // ────────────────── TOGGLE OTP ──────────────────
        [Authorize]
        [HttpPost("ToggleOTP")]
        public async Task<IActionResult> ToggleOTP([FromBody] JsonElement body)
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            bool otpEnabled = body.GetProperty("otpEnabled").GetBoolean();
            var result = await _userService.ToggleOtpAsync(reqId.Value, otpEnabled);
            
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize]
        [HttpPost("UploadAvatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Không có file nào được tải lên." });

            var reqId = GetRequesterId();
            if (reqId == null) return Unauthorized();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "Định dạng file không được hỗ trợ." });

            var fileName = $"avatar_{reqId}_{DateTime.Now.Ticks}{extension}";
            var path = Path.Combine(_env.WebRootPath, "uploads", "avatars", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{fileName}";
            
            var user = await _userService.GetByIdAsync(reqId.Value);
            if (user != null)
            {
                // Xóa file cũ nếu không phải là avatar mặc định
                if (!string.IsNullOrEmpty(user.Avatar) && !user.Avatar.StartsWith("http"))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, user.Avatar.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); } catch {}
                    }
                }

                await _userService.UpdateAsync(user.Id, new UpdateUserRequest { 
                    Name = user.Name, 
                    Role = user.Role, 
                    Avatar = avatarUrl,
                    Email = user.Email,
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Bio = user.Bio
                }, reqId.Value);
            }

            return Ok(new { success = true, avatarUrl });
        }

        // ────────────────── FORGOT PASSWORD ──────────────────
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null) 
                return BadRequest(new ApiResponse { Success = false, Message = "Email không tồn tại trong hệ thống." });

            string otp = _otpService.GenerateAndSaveOtp($"FORGOT_PW_{request.Email}", TimeSpan.FromMinutes(10));
            await _emailService.SendEmailAsync(request.Email, "Yêu cầu khôi phục mật khẩu",
                $"<h3>Chào {user.Name},</h3><p>Mã OTP để khôi phục mật khẩu của bạn là: <b style='font-size:28px;color:#dc3545'>{otp}</b></p><p>Mã có hiệu lực trong <b>10 phút</b>. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>");

            return Ok(new ApiResponse { Success = true, Message = "Mã OTP đã được gửi đến email." });
        }

        // ────────────────── RESET PASSWORD ──────────────────
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!_otpService.VerifyAndRemoveOtp($"FORGOT_PW_{request.Email}", request.Otp))
                return BadRequest(new ApiResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." });

            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null) 
                return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản không tồn tại." });

            await _userService.UpdatePasswordOnlyAsync(user, _authService.HashPassword(request.NewPassword));
            
            return Ok(new ApiResponse { Success = true, Message = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại." });
        }
        // ────────────────── CHANGE PASSWORD ──────────────────
        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var user = await _userService.GetByIdAsync(reqId.Value);
            if (user == null) return NotFound(new ApiResponse { Success = false, Message = "Không tìm thấy tài khoản." });

            if (!_authService.VerifyPassword(request.CurrentPassword, user))
                return BadRequest(new ApiResponse { Success = false, Message = "Mật khẩu hiện tại không chính xác." });

            // Cập nhật Database
            await _userService.UpdatePasswordOnlyAsync(user, _authService.HashPassword(request.NewPassword));

            // Thông báo bảo mật cho User
            await _notificationService.CreateNotificationAsync(
                reqId.Value,
                "Bảo mật tài khoản",
                "Mật khẩu của bạn đã được thay đổi thành công.",
                "success"
            );

            return Ok(new { success = true, message = "Đổi mật khẩu thành công!" });
        }
    }
}
