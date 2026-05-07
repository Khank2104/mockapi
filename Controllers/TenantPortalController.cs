using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "TenantOnly")]
    public class TenantPortalController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IRequestService _requestService;
        private readonly ITenantService _tenantService;

        public TenantPortalController(IInvoiceService invoiceService, IRequestService requestService, ITenantService tenantService)
        {
            _invoiceService = invoiceService;
            _requestService = requestService;
            _tenantService = tenantService;
        }

        private int GetRequesterId()
        {
            // Kiểm tra nhiều loại claim để đảm bảo tính tương thích
            var idClaim = User.FindFirst("id")?.Value 
                         ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.Identity?.Name;
            
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpGet("MyInvoices")]
        public async Task<IActionResult> GetMyInvoices()
        {
            var result = await _invoiceService.GetInvoicesByTenantAsync(GetRequesterId());
            return Ok(result);
        }

        [HttpGet("MyRoomInfo")]
        public async Task<IActionResult> GetMyRoomInfo()
        {
            var result = await _invoiceService.GetTenantRoomInfoAsync(GetRequesterId());
            return Ok(result);
        }

        [HttpGet("MyRequests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var result = await _requestService.GetTenantRequestsAsync(GetRequesterId());
            return Ok(result);
        }

        [HttpGet("GetInvoiceDetail/{id}")]
        public async Task<IActionResult> GetInvoiceDetail(int id)
        {
            var result = await _invoiceService.GetInvoiceByIdAsync(id, GetRequesterId());
            return result.Success ? Ok(result) : Forbid();
        }

        [HttpPost("CreateRequest")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateServiceRequest request)
        {
            var result = await _requestService.CreateRequestAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // --- Profile Management ---
        [HttpGet("GetMyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _tenantService.GetProfileByUserIdAsync(GetRequesterId());
            return Ok(result);
        }

        [HttpPut("UpdateMyProfile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateTenantProfileRequest request)
        {
            var result = await _tenantService.UpdateProfileByUserIdAsync(GetRequesterId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
