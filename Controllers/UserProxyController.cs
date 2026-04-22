using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using UserManagementSystem.Models;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _mockApiUrl;

        public UserProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _mockApiUrl = configuration["ExternalApis:MockApiUrl"];
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private async Task<User?> GetUserByIdAsync(string id)
        {
            var response = await _httpClient.GetAsync($"{_mockApiUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        private async Task<List<User>> GetAllUsersAsync()
        {
            var response = await _httpClient.GetAsync(_mockApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<User>();
            }
            return new List<User>();
        }


        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            Request.Headers.TryGetValue("X-Requester-Id", out var requesterId);
            if (string.IsNullOrEmpty(requesterId)) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            // TỐI ƯU: Gọi thẳng ID endpoint để check quyền nhanh hơn là load cả list
            var requester = await GetUserByIdAsync(requesterId!);
            if (requester == null || (requester.Role != "admin" && requester.Role != "superuser"))
                return StatusCode(403, new ApiResponse { Success = false, Message = "Bạn không có quyền truy cập danh sách người dùng." });

            var users = await GetAllUsersAsync();
            var response = users.Select(u => new UserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                Avatar = u.Avatar
            }).ToList();

            return Ok(response);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new ApiResponse { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });

            // TỐI ƯU: Sử dụng Filter của MockAPI để tìm User theo Username nhanh hơn
            var response = await _httpClient.GetAsync($"{_mockApiUrl}?username={request.Username}");
            if (!response.IsSuccessStatusCode)
                return Unauthorized(new ApiResponse { Success = false, Message = "Lỗi kết nối MockAPI." });

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            string hashedPassword = HashPassword(request.Password);
            var user = users?.FirstOrDefault(u => 
                u.Username == request.Username && 
                (u.Password == hashedPassword || u.Password == request.Password)
            );

            if (user != null)
            {
                var userResponse = new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    Avatar = user.Avatar
                };
                return Ok(new ApiResponse { Success = true, Data = userResponse });
            }

            return Unauthorized(new ApiResponse { Success = false, Message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new ApiResponse { Success = false, Message = "Thông tin không đầy đủ." });

            // Check trùng bằng filter cho nhanh
            var checkResponse = await _httpClient.GetAsync($"{_mockApiUrl}?username={request.Username}");
            var checkContent = await checkResponse.Content.ReadAsStringAsync();
            var existingUsers = JsonSerializer.Deserialize<List<User>>(checkContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (existingUsers != null && existingUsers.Any())
                return BadRequest(new ApiResponse { Success = false, Message = "Tên đăng nhập đã tồn tại." });

            var newUser = new User
            {
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                Password = HashPassword(request.Password),
                Role = "user", 
                Avatar = ""
            };

            var response = await _httpClient.PostAsJsonAsync(_mockApiUrl, newUser);
            if (response.IsSuccessStatusCode)
                return Ok(new ApiResponse { Success = true, Message = "Đăng ký thành công." });

            return BadRequest(new ApiResponse { Success = false, Message = "Lỗi khi đăng ký từ MockAPI." });
        }

        [HttpPost("CreateByAdmin")]
        public async Task<IActionResult> CreateByAdmin([FromBody] CreateUserRequest request)
        {
            Request.Headers.TryGetValue("X-Requester-Id", out var requesterId);
            if (string.IsNullOrEmpty(requesterId)) return Unauthorized(new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var requester = await GetUserByIdAsync(requesterId!);
            if (requester == null || (requester.Role != "admin" && requester.Role != "superuser"))
                return StatusCode(403, new ApiResponse { Success = false, Message = "Quyền hạn không đủ." });

            if (requester.Role == "admin" && request.Role != "user")
                return BadRequest(new ApiResponse { Success = false, Message = "Admin chỉ có quyền tạo tài khoản Role: User." });
            
            if (request.Role == "superuser")
                return BadRequest(new ApiResponse { Success = false, Message = "Không thể tạo thêm tài khoản Superuser." });

            // Check trùng
            var checkResponse = await _httpClient.GetAsync($"{_mockApiUrl}?username={request.Username}");
            var checkContent = await checkResponse.Content.ReadAsStringAsync();
            var existingUsers = JsonSerializer.Deserialize<List<User>>(checkContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (existingUsers != null && existingUsers.Any())
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

            var response = await _httpClient.PostAsJsonAsync(_mockApiUrl, newUser);
            if (response.IsSuccessStatusCode)
                return Ok(new ApiResponse { Success = true, Message = "Tạo người dùng thành công." });

            return BadRequest(new ApiResponse { Success = false, Message = "Lỗi MockAPI." });
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
        {
            Request.Headers.TryGetValue("X-Requester-Id", out var requesterId);
            if (string.IsNullOrEmpty(requesterId)) return StatusCode(401, new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            // Chạy song song 2 request check info để tiết kiệm thời gian
            var taskRequester = GetUserByIdAsync(requesterId!);
            var taskTarget = GetUserByIdAsync(id);
            await Task.WhenAll(taskRequester, taskTarget);

            var requester = await taskRequester;
            var targetUser = await taskTarget;

            if (targetUser == null || requester == null) return NotFound(new ApiResponse { Success = false, Message = "Dữ hiệu không hợp lệ." });

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
                
                // RULE MỚI: Admin không được tự đổi role của chính mình
                if (requester.Id == targetUser.Id && request.Role != targetUser.Role)
                    return BadRequest(new ApiResponse { Success = false, Message = "Admin không được tự hạ quyền hoặc thay đổi Role của chính mình." });
            }


            if (targetUser.Role == "superuser" && request.Role != "superuser")
                return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản Superuser là cố định." });

            var updatedUser = new User
            {
                Id = id,
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                Role = request.Role,
                Avatar = request.Avatar ?? targetUser.Avatar,
                Password = !string.IsNullOrEmpty(request.Password) ? HashPassword(request.Password) : targetUser.Password
            };

            var response = await _httpClient.PutAsJsonAsync($"{_mockApiUrl}/{id}", updatedUser);
            if (response.IsSuccessStatusCode) return Ok(new ApiResponse { Success = true, Message = "Cập nhật thành công." });
            
            return BadRequest(new ApiResponse { Success = false, Message = "Lỗi khi cập nhật MockAPI." });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            Request.Headers.TryGetValue("X-Requester-Id", out var requesterId);
            if (string.IsNullOrEmpty(requesterId)) return StatusCode(401, new ApiResponse { Success = false, Message = "Bạn cần đăng nhập." });

            var taskRequester = GetUserByIdAsync(requesterId!);
            var taskTarget = GetUserByIdAsync(id);
            await Task.WhenAll(taskRequester, taskTarget);

            var requester = await taskRequester;
            var targetUser = await taskTarget;

            if (targetUser == null || requester == null) return NotFound(new ApiResponse { Success = false, Message = "Dữ liệu không hợp lệ." });

            if (targetUser.Role == "superuser")
                return BadRequest(new ApiResponse { Success = false, Message = "Tài khoản Superuser là bất tử." });

            if (requester.Role == "user")
                return StatusCode(403, new ApiResponse { Success = false, Message = "Bạn không có quyền xóa." });

            if (requester.Role == "admin" && targetUser.Role != "user")
                return StatusCode(403, new ApiResponse { Success = false, Message = "Admin chỉ có quyền xóa User thường." });

            if (requester.Id == id)
                return BadRequest(new ApiResponse { Success = false, Message = "Không thể tự xóa chính mình." });

            var response = await _httpClient.DeleteAsync($"{_mockApiUrl}/{id}");
            if (response.IsSuccessStatusCode) return Ok(new ApiResponse { Success = true, Message = "Xóa người dùng thành công." });
            
            return BadRequest(new ApiResponse { Success = false, Message = "Lỗi MockAPI." });
        }




    }
}

