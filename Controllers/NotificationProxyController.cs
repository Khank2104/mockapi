using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationProxyController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationProxyController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int? GetRequesterId()
        {
            var idClaim = User.FindFirst("id")?.Value;
            if (int.TryParse(idClaim, out int reqId)) return reqId;
            return null;
        }

        [HttpGet("GetMyNotifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized();

            var list = await _notificationService.GetUserNotificationsAsync(reqId.Value);
            var unreadCount = await _notificationService.GetUnreadCountAsync(reqId.Value);

            return Ok(new { success = true, data = list, unreadCount });
        }

        [HttpPost("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized();

            var success = await _notificationService.MarkAsReadAsync(id, reqId.Value);
            return Ok(new { success });
        }

        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int? reqId = GetRequesterId();
            if (reqId == null) return Unauthorized();

            var success = await _notificationService.MarkAllAsReadAsync(reqId.Value);
            return Ok(new { success });
        }
    }
}
