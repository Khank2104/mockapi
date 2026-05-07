using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Management")]
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

        [HttpGet("Summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int month, [FromQuery] int year, [FromQuery] int? motelId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _invoiceService.GetBillingSummaryAsync(month, year, GetRequesterId(), motelId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}/ExportExcel")]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var fileBytes = await _invoiceService.ExportInvoiceToExcelAsync(id, GetRequesterId());
            if (fileBytes == null || fileBytes.Length == 0) return NotFound("Invoice not found or access denied.");

            var fileName = $"HoaDon_Phong_{id}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, GetRequesterId());
            if (!invoice.Success) return Forbid();
            
            // Delete invoice logic using DB context directly for quick admin action
            var db = HttpContext.RequestServices.GetService<UserManagementSystem.Data.ApplicationDbContext>();
            if (db == null) return StatusCode(500, "Database connection error");

            var inv = await db.Invoices.FindAsync(id);
            if (inv != null) {
                var details = db.InvoiceDetails.Where(d => d.InvoiceId == id).ToList();
                db.InvoiceDetails.RemoveRange(details);
                db.Invoices.Remove(inv);
                await db.SaveChangesAsync();
                return Ok(new ApiResponse { Success = true, Message = "Đã xóa hóa đơn." });
            }
            return NotFound();
        }
    }
}
