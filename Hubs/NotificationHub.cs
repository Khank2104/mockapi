using Microsoft.AspNetCore.SignalR;

namespace UserManagementSystem.Hubs
{
    public class NotificationHub : Hub
    {
        // Khi một user kết nối, tự động đưa họ vào Group cá nhân và Group Role
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var userId = user?.FindFirst("id")?.Value;
            var role = user?.FindFirst("role")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
            }

            await base.OnConnectedAsync();
        }

        // Gửi thông báo đến tất cả mọi người (Dùng cho thông báo hệ thống toàn cục)
        public async Task BroadcastMessage(string title, string message, string type = "info")
        {
            await Clients.All.SendAsync("ReceiveNotification", title, message, type);
        }

        // Gửi thông báo đến một user cụ thể thông qua Group (An toàn hơn Clients.User mặc định)
        public async Task SendToUserGroup(string userId, string title, string message, string type = "info")
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", title, message, type);
        }
    }
}
