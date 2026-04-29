using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

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
            Id = u.UserId.ToString(),
            Name = u.Name,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role?.RoleName ?? "tenant",
            Avatar = u.Avatar,
            Phone = u.Phone,
            Status = u.Status
        };

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int reqId) ? reqId : 0;
        }

        [Authorize]
        [HttpGet("GetMyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var reqId = GetRequesterId();
            if (reqId == 0) return Unauthorized();

            var user = await _userService.GetByIdAsync(reqId);
            if (user == null) return NotFound();

            return Ok(new { success = true, data = ToResponse(user) });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new ApiResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });

            var user = await _userService.GetByUsernameAsync(request.Username);

            if (user != null && user.Status != "Active")
                return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản của bạn đã bị khóa." });

            if (user != null && _authService.VerifyPassword(request.Password, user))
            {
                string token = _authService.GenerateJwtToken(user);
                SetTokenCookie(token);
                return Ok(new ApiResponse { Success = true, Data = new { User = ToResponse(user) } });
            }

            return Unauthorized(new ApiResponse { Success = false, Message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("X-Access-Token");
            return Ok(new ApiResponse { Success = true, Message = "Đã đăng xuất." });
        }

        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
        {
            var reqId = GetRequesterId();
            var result = await _userService.UpdateAsync(reqId, request, reqId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost("UploadAvatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Không có file nào được tải lên." });

            var reqId = GetRequesterId();
            if (reqId == 0) return Unauthorized();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "Định dạng file không được hỗ trợ." });

            var fileName = $"avatar_{reqId}_{DateTime.Now.Ticks}{extension}";
            var path = Path.Combine(_env.WebRootPath, "uploads", "avatars", fileName);

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{fileName}";
            
            var user = await _userService.GetByIdAsync(reqId);
            if (user != null)
            {
                await _userService.UpdateAsync(user.UserId, new UpdateUserRequest { 
                    Name = user.Name, 
                    Email = user.Email,
                    Username = user.Username,
                    Phone = user.Phone,
                    Avatar = avatarUrl
                }, reqId);
            }

            return Ok(new { success = true, avatarUrl });
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null) 
                return BadRequest(new ApiResponse { Success = false, Message = "Email không tồn tại trong hệ thống." });

            string otp = _otpService.GenerateAndSaveOtp($"FORGOT_PW_{request.Email}", TimeSpan.FromMinutes(10));
            await _emailService.SendEmailAsync(request.Email, "Yêu cầu khôi phục mật khẩu",
                $"<h3>Chào {user.Name},</h3><p>Mã OTP để khôi phục mật khẩu của bạn là: <b style='font-size:28px;color:#dc3545'>{otp}</b></p>");

            return Ok(new ApiResponse { Success = true, Message = "Mã OTP đã được gửi đến email." });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!_otpService.VerifyAndRemoveOtp($"FORGOT_PW_{request.Email}", request.Otp))
                return BadRequest(new ApiResponse { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." });

            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null) return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản không tồn tại." });

            await _userService.UpdatePasswordOnlyAsync(user, _authService.HashPassword(request.NewPassword));
            return Ok(new ApiResponse { Success = true, Message = "Đổi mật khẩu thành công!" });
        }

        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var reqId = GetRequesterId();
            var user = await _userService.GetByIdAsync(reqId);
            if (user == null) return NotFound();

            if (!_authService.VerifyPassword(request.CurrentPassword, user))
                return BadRequest(new ApiResponse { Success = false, Message = "Mật khẩu hiện tại không chính xác." });

            await _userService.UpdatePasswordOnlyAsync(user, _authService.HashPassword(request.NewPassword));
            return Ok(new { success = true, message = "Đổi mật khẩu thành công!" });
        }
    }
}
