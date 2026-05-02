using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Management")]
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

        /// <summary>
        /// Create a new motel property
        /// </summary>
        /// <param name="request">Motel details (name, address, description, etc.)</param>
        /// <returns>Created motel information with ID</returns>
        /// <response code="200">Motel created successfully</response>
        /// <response code="400">Invalid input or business rule violation</response>
        /// <response code="401">Unauthorized</response>
        // --- Motel ---
        [HttpPost("CreateMotel")]
        public async Task<IActionResult> CreateMotel([FromBody] MotelRequest request)
        {
            var result = await _motelService.CreateMotelAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get all motels owned by the authenticated admin user
        /// </summary>
        /// <returns>List of motel properties</returns>
        /// <response code="200">Returns list of motels</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("MyMotels")]
        public async Task<IActionResult> GetMyMotels()
        {
            var result = await _motelService.GetMotelsByAdminAsync(GetRequesterId());
            return Ok(result);
        }

        /// <summary>
        /// Create a new floor in a motel
        /// </summary>
        /// <param name="request">Floor details (motel ID, floor number, name, etc.)</param>
        /// <returns>Created floor information</returns>
        /// <response code="200">Floor created successfully</response>
        /// <response code="400">Invalid input or motel not found</response>
        /// <response code="401">Unauthorized</response>
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

        // --- Global Services ---
        [HttpGet("GetGlobalServices")]
        public async Task<IActionResult> GetGlobalServices()
        {
            var result = await _motelService.GetGlobalServicesAsync();
            return Ok(result);
        }

        [HttpPost("UpdateGlobalService/{serviceId}")]
        public async Task<IActionResult> UpdateGlobalService(int serviceId, [FromBody] GlobalServiceUpdateRequest request)
        {
            var result = await _motelService.UpdateGlobalServiceAsync(serviceId, request.DefaultPrice, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("CreateGlobalService")]
        public async Task<IActionResult> CreateGlobalService([FromBody] ServiceRequest request)
        {
            var result = await _motelService.CreateGlobalServiceAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("SeedDefaultServices")]
        public async Task<IActionResult> SeedDefaultServices()
        {
            var result = await _motelService.SeedDefaultServicesAsync(GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
