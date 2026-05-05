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
                .Include(c => c.Room)
                    .ThenInclude(r => r.Occupants)
                .Include(c => c.PrimaryTenant)
                .Where(c => c.ContractStatus == "Active" && c.EndDate.HasValue && c.EndDate.Value < now)
                .ToListAsync();

            if (newlyExpiredContracts.Any())
            {
                foreach (var contract in newlyExpiredContracts)
                {
                    contract.ContractStatus = "Waiting";
                    if (contract.Room != null)
                    {
                        contract.Room.Status = "Vacant";
                        // Cập nhật tất cả người ở trong phòng này thành đã dời đi
                        foreach (var occ in contract.Room.Occupants.Where(o => o.CheckOutDate == null))
                        {
                            occ.Status = "MovedOut";
                            occ.CheckOutDate = now;
                            occ.UpdatedAt = now;
                        }
                    }
                    if (contract.PrimaryTenant != null)
                    {
                        contract.PrimaryTenant.TenantStatus = "MovedOut";
                        contract.PrimaryTenant.UpdatedAt = now;
                    }
                    contract.UpdatedAt = now;
                    _logger.LogInformation($"Contract {contract.ContractId} moved to Waiting state (expired on {contract.EndDate}).");
                }
                await db.SaveChangesAsync();
            }

            // 2. Chuyển sang trạng thái "Terminated" thay vì xóa dữ liệu nếu ở "Waiting" quá 7 ngày
            var pendingTerminationContracts = await db.Contracts
                .Where(c => c.ContractStatus == "Waiting" && c.EndDate.HasValue && c.EndDate.Value.AddDays(7) < now)
                .ToListAsync();

            if (pendingTerminationContracts.Any())
            {
                foreach (var contract in pendingTerminationContracts)
                {
                    _logger.LogInformation($"Terminating Contract {contract.ContractId} (Waiting > 7 days). Preserving all financial data.");

                    contract.ContractStatus = "Terminated";
                    contract.UpdatedAt = now;

                    // Đảm bảo phòng ở trạng thái Trống (Vacant)
                    var room = await db.Rooms.FindAsync(contract.RoomId);
                    if (room != null && room.Status != "Vacant")
                    {
                        room.Status = "Vacant";
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
