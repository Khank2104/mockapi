using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Services;
using System.Security.Claims;

namespace UserManagementSystem.Controllers
{
    public class QRPaymentController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;
        private readonly IWebHostEnvironment _env;

        public QRPaymentController(IInvoiceService invoiceService, IPaymentService paymentService, IWebHostEnvironment env)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _env = env;
        }

        [AllowAnonymous]
        [HttpGet("/QRPayment/{id:int}")]
        public IActionResult Index(int id)
        {
            return View(id);
        }

        [AllowAnonymous]
        [HttpGet("/api/QRPayment/{id:int}/info")]
        public async Task<IActionResult> GetPublicInfo(int id)
        {
            var userIdStr = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.TryParse(userIdStr, out int uid) ? uid : 0;

            // We use the service which handles DB logic
            var result = await _invoiceService.GetInvoiceByIdAsync(id, userId); 
            if (!result.Success) return result.Message.Contains("quyền") ? Forbid(result.Message) : NotFound(result);

            dynamic inv = result.Data!;
            var transferNote = $"P{inv.RoomCode} T{inv.BillingMonth}{inv.BillingYear}";

            return Ok(new
            {
                roomCode = inv.RoomCode,
                billingMonth = inv.BillingMonth,
                billingYear = inv.BillingYear,
                totalAmount = inv.TotalAmount,
                status = inv.Status,
                transferNote = transferNote,
                bankName = "Techcombank",
                accountName = "NGUYEN NGOC KHANH",
                accountNumber = "20040363479028",
                bankId = "TCB"
            });
        }

        [Authorize(Policy = "TenantOnly")]
        [HttpPost("/api/QRPayment/SubmitProof")]
        public async Task<IActionResult> SubmitProof([FromForm] int invoiceId, IFormFile proofFile)
        {
            if (proofFile == null || proofFile.Length == 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn ảnh minh chứng." });

            var userIdStr = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập lại." });
            
            var userId = int.Parse(userIdStr);
            
            // Save file
            string fileName = $"proof_{invoiceId}_{DateTime.Now.Ticks}{Path.GetExtension(proofFile.FileName)}";
            string uploadPath = Path.Combine(_env.WebRootPath, "uploads", "proofs");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            
            string filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await proofFile.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/proofs/{fileName}";
            var result = await _paymentService.SubmitPaymentProofAsync(invoiceId, relativePath, userId);
            
            return Ok(result);
        }

        [Authorize(Policy = "Management")]
        [HttpPost("/api/QRPayment/Verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyRequest request)
        {
            var adminIdStr = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr)) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập lại." });
            
            var adminId = int.Parse(adminIdStr);
            var result = await _paymentService.VerifyPaymentAsync(request.InvoiceId, request.Approved, adminId, request.ActualAmount);
            return Ok(result);
        }

        public class VerifyRequest
        {
            public int InvoiceId { get; set; }
            public bool Approved { get; set; }
            public decimal? ActualAmount { get; set; }
        }
    }
}
