using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,superuser")]
    public class MotelManagementController : ControllerBase
    {
        private readonly IMotelService _motelService;

        public MotelManagementController(IMotelService motelService)
        {
            _motelService = motelService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // --- Motel ---
        [HttpPost("CreateMotel")]
        public async Task<IActionResult> CreateMotel([FromBody] MotelRequest request)
        {
            var result = await _motelService.CreateMotelAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("MyMotels")]
        public async Task<IActionResult> GetMyMotels()
        {
            var result = await _motelService.GetMotelsByAdminAsync(GetRequesterId());
            return Ok(result);
        }

        // --- Floor ---
        [HttpPost("CreateFloor")]
        public async Task<IActionResult> CreateFloor([FromBody] FloorRequest request)
        {
            var result = await _motelService.CreateFloorAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // --- Room ---
        [HttpPost("CreateRoom")]
        public async Task<IActionResult> CreateRoom([FromBody] RoomRequest request)
        {
            var result = await _motelService.CreateRoomAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // --- Configuration ---
        [HttpPost("UpdateRoomSetting")]
        public async Task<IActionResult> UpdateRoomSetting([FromBody] RoomSettingRequest request)
        {
            var result = await _motelService.UpdateRoomSettingAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("UpdateServiceSetting")]
        public async Task<IActionResult> UpdateServiceSetting([FromBody] RoomServiceSettingRequest request)
        {
            var result = await _motelService.UpdateRoomServiceSettingAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetRoomSettings/{roomId}")]
        public async Task<IActionResult> GetRoomSettings(int roomId)
        {
            var result = await _motelService.GetRoomSettingsAsync(roomId, GetRequesterId());
            return Ok(result);
        }

        [HttpGet("GetRoomServices/{roomId}")]
        public async Task<IActionResult> GetRoomServices(int roomId)
        {
            var result = await _motelService.GetRoomServicesAsync(roomId, GetRequesterId());
            return Ok(result);
        }

        [HttpGet("GetRoomOccupants/{roomId}")]
        public async Task<IActionResult> GetRoomOccupants(int roomId)
        {
            var result = await _motelService.GetRoomOccupantsAsync(roomId, GetRequesterId());
            return Ok(result);
        }
    }
}
