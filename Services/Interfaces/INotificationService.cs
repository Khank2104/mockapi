using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string title, string message, string type = "info");
        Task CreateNotificationForRolesAsync(string[] roles, string title, string message, string type = "info");
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int count = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
    }
}
