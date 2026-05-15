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
        private readonly IExportService _exportService;

        public InvoiceController(IInvoiceService invoiceService, IExportService exportService)
        {
            _invoiceService = invoiceService;
            _exportService = exportService;
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

        [HttpGet("FinancialDashboard")]
        public async Task<IActionResult> GetFinancialDashboard([FromQuery] int month, [FromQuery] int year, [FromQuery] int? motelId = null)
        {
            var result = await _invoiceService.GetDashboardFinancialSummaryAsync(month, year, GetRequesterId(), motelId);
            return Ok(result);
        }

        [HttpGet("{id}/ExportExcel")]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var fileBytes = await _exportService.ExportInvoiceToExcelAsync(id, GetRequesterId());
            if (fileBytes == null || fileBytes.Length == 0) return NotFound("Invoice not found or access denied.");

            var fileName = $"HoaDon_Phong_{id}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
