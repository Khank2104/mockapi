using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using UserManagementSystem.Data;
using UserManagementSystem.Hubs;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, string type = "info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // Chỉ gửi cho user đích thông qua Group (Bảo mật luồng dữ liệu)
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("NewNotification", userId);
        }

        public async Task CreateNotificationForRolesAsync(string[] roles, string title, string message, string type = "info")
        {
            var users = await _db.Users.Where(u => roles.Contains(u.Role.RoleName)).ToListAsync();
            var notifications = new List<Notification>();

            foreach (var user in users)
            {
                notifications.Add(new Notification
                {
                    UserId = user.UserId,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    IsRead = false
                });
            }

            if (notifications.Any())
            {
                _db.Notifications.AddRange(notifications);
                await _db.SaveChangesAsync();
                
                // Chỉ gửi cho các Role liên quan thông qua Groups
                foreach (var role in roles)
                {
                    await _hubContext.Clients.Group($"Role_{role}").SendAsync("NewRoleNotification", roles);
                }
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int count = 20)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            if (unread.Any())
            {
                foreach(var n in unread) n.IsRead = true;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
