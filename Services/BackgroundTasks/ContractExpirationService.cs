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
    public class ContractExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContractExpirationService> _logger;

        public ContractExpirationService(IServiceProvider serviceProvider, ILogger<ContractExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Contract Expiration Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredContracts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing expired contracts.");
                }

                // Check every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessExpiredContracts()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;

            // 1. Chuyển hợp đồng hết hạn sang trạng thái "Waiting"
            var newlyExpiredContracts = await db.Contracts
                .Where(c => c.ContractStatus == "Active" && c.EndDate.HasValue && c.EndDate.Value < now)
                .ToListAsync();

            if (newlyExpiredContracts.Any())
            {
                foreach (var contract in newlyExpiredContracts)
                {
                    contract.ContractStatus = "Waiting";
                    contract.UpdatedAt = now;
                    _logger.LogInformation($"Contract {contract.ContractId} moved to Waiting state (expired on {contract.EndDate}).");
                }
                await db.SaveChangesAsync();
            }

            // 2. Xóa sạch dữ liệu nếu ở trạng thái "Waiting" quá 7 ngày
            var pendingDeletionContracts = await db.Contracts
                .Where(c => c.ContractStatus == "Waiting" && c.EndDate.HasValue && c.EndDate.Value.AddDays(7) < now)
                .ToListAsync();

            if (pendingDeletionContracts.Any())
            {
                foreach (var contract in pendingDeletionContracts)
                {
                    _logger.LogInformation($"Deleting all data for Contract {contract.ContractId} (Waiting > 7 days).");

                    int roomId = contract.RoomId;

                    // Xóa Invoices
                    var invoices = await db.Invoices.Where(i => i.ContractId == contract.ContractId).ToListAsync();
                    db.Invoices.RemoveRange(invoices);

                    // Xóa RoomOccupants
                    var occupants = await db.RoomOccupants.Where(o => o.RoomId == roomId).ToListAsync();
                    var tenantIds = occupants.Select(o => o.TenantId).ToList();

                    // Xóa Requests
                    if (tenantIds.Any())
                    {
                        var requests = await db.Requests.Where(r => tenantIds.Contains(r.TenantId)).ToListAsync();
                        db.Requests.RemoveRange(requests);
                    }

                    db.RoomOccupants.RemoveRange(occupants);
                    db.Contracts.Remove(contract);

                    // Xóa Tenants và Users
                    var tenants = await db.Tenants.Where(t => tenantIds.Contains(t.TenantId)).ToListAsync();
                    var userIds = tenants.Where(t => t.UserId != null).Select(t => t.UserId.Value).ToList();
                    db.Tenants.RemoveRange(tenants);

                    if (userIds.Any())
                    {
                        var users = await db.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();
                        db.Users.RemoveRange(users);
                    }

                    // Đặt phòng lại trạng thái Trống (Vacant)
                    var room = await db.Rooms.FindAsync(roomId);
                    if (room != null)
                    {
                        room.Status = "Vacant";
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
