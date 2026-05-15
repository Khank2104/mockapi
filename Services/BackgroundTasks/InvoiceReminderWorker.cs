using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UserManagementSystem.Data;

namespace UserManagementSystem.Services.BackgroundTasks
{
    public class InvoiceReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InvoiceReminderWorker> _logger;

        public InvoiceReminderWorker(IServiceProvider serviceProvider, ILogger<InvoiceReminderWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Invoice Reminder Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Chạy vào buổi sáng (ví dụ: giờ hiện tại >= 7h sáng)
                    // Nếu cần có thể check cụ thể giờ, ở đây cứ chạy mỗi 1 tiếng 1 lần
                    await ProcessOverdueInvoices();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing overdue invoices.");
                }

                // Check every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessOverdueInvoices()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.Now;

            // Tìm các hóa đơn chưa thanh toán và đã quá hạn (thêm 1 ngày ân hạn)
            var overdueInvoices = await db.Invoices
                .Include(i => i.Room).ThenInclude(r => r.Motel)
                .Include(i => i.PrimaryTenant).ThenInclude(t => t.User)
                .Where(i => i.InvoiceStatus == "Unpaid" && i.DueDate.AddDays(1) < now)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                if (invoice.PrimaryTenant?.User == null) continue;
                
                var userId = invoice.PrimaryTenant.User.UserId;
                var userEmail = invoice.PrimaryTenant.User.Email;
                var roomCode = invoice.Room.RoomCode;
                var today = DateTime.Today;

                // Kiểm tra xem hôm nay đã gửi thông báo nhắc nợ cho hóa đơn này chưa
                // Dựa vào title và date
                var alreadyNotifiedToday = await db.Notifications.AnyAsync(n => 
                    n.UserId == userId && 
                    n.Title == "Nhắc nhở thanh toán hóa đơn" && 
                    n.CreatedAt.Date == today);

                if (alreadyNotifiedToday) continue; // Đã gửi rồi thì bỏ qua

                // Tạo thông báo trong app
                string notifMessage = $"Hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear} (Phòng {roomCode}) của bạn đã quá hạn. Vui lòng thanh toán sớm.";
                await notifService.CreateNotificationAsync(userId, "Nhắc nhở thanh toán hóa đơn", notifMessage, "warning");

                // Gửi Email
                if (!string.IsNullOrEmpty(userEmail))
                {
                    string subject = $"[QuanTro] Nhắc nhở thanh toán hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear}";
                    string body = $@"
                        <h3>Xin chào {invoice.PrimaryTenant.FullName},</h3>
                        <p>Hệ thống ghi nhận hóa đơn tiền phòng tháng <b>{invoice.BillingMonth}/{invoice.BillingYear}</b> của phòng <b>{roomCode}</b> đã quá hạn thanh toán.</p>
                        <ul>
                            <li><b>Hạn thanh toán:</b> {invoice.DueDate.ToString("dd/MM/yyyy")}</li>
                            <li><b>Tổng tiền cần thanh toán:</b> <span style='color:red;font-size:18px;font-weight:bold;'>{invoice.TotalAmount:N0} VNĐ</span></li>
                        </ul>
                        <p>Vui lòng sắp xếp thanh toán sớm để không bị ảnh hưởng đến dịch vụ và phát sinh phí phạt (nếu có).</p>
                        <p>Nếu bạn đã thanh toán, xin vui lòng bỏ qua email này.</p>
                        <br/>
                        <p>Trân trọng,<br/>Ban quản lý khu trọ {invoice.Room.Motel.MotelName}</p>
                    ";
                    
                    try
                    {
                        await emailService.SendEmailAsync(userEmail, subject, body);
                        _logger.LogInformation($"Sent overdue reminder email to {userEmail} for Invoice {invoice.InvoiceId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send reminder email to {userEmail}");
                    }
                }
            }
        }
    }
}
