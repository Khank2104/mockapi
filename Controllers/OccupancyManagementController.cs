using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OccupancyManagementController : ControllerBase
    {
        private readonly IOccupancyService _occupancyService;

        public OccupancyManagementController(IOccupancyService occupancyService)
        {
            _occupancyService = occupancyService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpPost("AddOccupant")]
        public async Task<IActionResult> AddOccupant([FromBody] RoomOccupantRequest request)
        {
            var result = await _occupancyService.AddOccupantAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("RemoveOccupant/{id}")]
        public async Task<IActionResult> RemoveOccupant(int id)
        {
            var result = await _occupancyService.RemoveOccupantAsync(id, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("CreateContract")]
        public async Task<IActionResult> CreateContract([FromBody] ContractRequest request)
        {
            var result = await _occupancyService.CreateContractAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("TerminateContract/{id}")]
        public async Task<IActionResult> TerminateContract(int id)
        {
            var result = await _occupancyService.TerminateContractAsync(id, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
