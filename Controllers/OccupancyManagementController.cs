using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Management")]
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

        [HttpGet("GetAllContracts")]
        public async Task<IActionResult> GetAllContracts([FromQuery] int? motelId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _occupancyService.GetAllContractsAsync(GetRequesterId(), motelId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("GetActiveContractByRoom/{roomId}")]
        public async Task<IActionResult> GetActiveContractByRoom(int roomId)
        {
            var result = await _occupancyService.GetActiveContractByRoomAsync(roomId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPut("UpdateContract/{id}")]
        public async Task<IActionResult> UpdateContract(int id, [FromBody] ContractRequest request)
        {
            var result = await _occupancyService.UpdateContractAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
