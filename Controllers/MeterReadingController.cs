using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,superuser")]
    public class MeterReadingController : ControllerBase
    {
        private readonly IMeterReadingService _readingService;

        public MeterReadingController(IMeterReadingService readingService)
        {
            _readingService = readingService;
        }

        private int GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateMeterReadingRequest request)
        {
            var result = await _readingService.CreateReadingAsync(request, GetRequesterId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Room/{roomId}")]
        public async Task<IActionResult> GetByRoom(int roomId, [FromQuery] int month, [FromQuery] int year)
        {
            var result = await _readingService.GetReadingsByRoomAsync(roomId, month, year, GetRequesterId());
            return Ok(result);
        }
    }
}
