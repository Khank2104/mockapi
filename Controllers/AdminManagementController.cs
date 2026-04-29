using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "superuser")]
    public class AdminManagementController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminManagementController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpPost("CreateAdmin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            var result = await _adminService.CreateAdminAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("ListAdmins")]
        public async Task<IActionResult> ListAdmins()
        {
            var result = await _adminService.GetAllAdminsAsync(GetRequesterId());
            return result.Success ? Ok(result) : Forbid();
        }

        [HttpPost("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, [FromQuery] bool active)
        {
            var result = await _adminService.ToggleAdminStatusAsync(id, active, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
