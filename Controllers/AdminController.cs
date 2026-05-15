using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Authorize(Policy = "Management")]
    public class AdminController : Controller
    {
        private readonly IContractService _contractService;

        public AdminController(IContractService contractService)
        {
            _contractService = contractService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Admin/Contract/Print/{id}")]
        public async Task<IActionResult> PrintContract(int id)
        {
            var adminIdClaim = User.FindFirst("id")?.Value;
            if (!int.TryParse(adminIdClaim, out int adminId)) return Forbid();

            var result = await _contractService.GetContractForPrintAsync(id, adminId);
            if (!result.Success) return NotFound(result.Message);

            return View(result.Data); // Returns PrintContract.cshtml
        }
    }
}
