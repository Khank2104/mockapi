using Microsoft.AspNetCore.SignalR;

namespace UserManagementSystem.Hubs
{
    public class NotificationHub : Hub
    {
        // Gửi thông báo đến tất cả mọi người
        public async Task BroadcastMessage(string title, string message, string type = "info")
        {
            await Clients.All.SendAsync("ReceiveNotification", title, message, type);
        }

        // Gửi thông báo đến một user cụ thể
        public async Task SendToUser(string userId, string title, string message, string type = "info")
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message, type);
        }
    }
}
