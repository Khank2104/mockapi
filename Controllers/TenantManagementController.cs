using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Management")]
    public class TenantManagementController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantManagementController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // --- Profile Management ---
        [HttpPost("CreateProfile")]
        public async Task<IActionResult> CreateProfile([FromBody] CreateTenantProfileRequest request)
        {
            var result = await _tenantService.CreateProfileAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("UpdateProfile/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateTenantProfileRequest request)
        {
            var result = await _tenantService.UpdateProfileAsync(id, request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetProfile/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var result = await _tenantService.GetProfileByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("GetAllTenants")]
        public async Task<IActionResult> GetAllTenants()
        {
            var result = await _tenantService.GetAllProfilesAsync(GetRequesterId());
            return result.Success ? Ok(result) : Forbid();
        }

        // --- Integrated Management ---
        [HttpPost("CreateTenant")]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
        {
            // Logic to create both profile and account
            var result = await _tenantService.CreateTenantFullAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
