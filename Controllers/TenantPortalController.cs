using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TenantPortalController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IRequestService _requestService;

        public TenantPortalController(IInvoiceService invoiceService, IRequestService requestService)
        {
            _invoiceService = invoiceService;
            _requestService = requestService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpGet("MyInvoices")]
        public async Task<IActionResult> GetMyInvoices()
        {
            var result = await _invoiceService.GetInvoicesByTenantAsync(GetRequesterId());
            return Ok(result);
        }

        [HttpGet("MyRequests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var result = await _requestService.GetTenantRequestsAsync(GetRequesterId());
            return Ok(result);
        }

        [HttpPost("CreateRequest")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateTenantRequest request)
        {
            var result = await _requestService.CreateRequestAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
