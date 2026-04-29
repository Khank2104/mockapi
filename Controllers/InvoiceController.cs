using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpPost("Generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateInvoiceRequest request)
        {
            var result = await _invoiceService.GenerateInvoiceAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _invoiceService.GetInvoiceByIdAsync(id, GetRequesterId());
            return result.Success ? Ok(result) : Forbid();
        }

        [HttpGet("Room/{roomId}")]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var result = await _invoiceService.GetInvoicesByRoomAsync(roomId, GetRequesterId());
            return Ok(result);
        }
    }
}
