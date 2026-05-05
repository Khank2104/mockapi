using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class GlobalServiceService : IGlobalServiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IAccessControlService _accessControl;

        public GlobalServiceService(ApplicationDbContext db, INotificationService notificationService, IAccessControlService accessControl)
        {
            _db = db;
            _notificationService = notificationService;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> GetGlobalServicesAsync()
        {
            var services = await _db.Services
                .Where(s => s.IsSystemDefault && s.IsActive)
                .Select(s => new
                {
                    s.ServiceId,
                    s.ServiceName,
                    s.ServiceCode,
                    s.Unit,
                    s.DefaultPrice,
                    s.CalculationType
                })
                .ToListAsync();

            return new ApiResponse { Success = true, Data = services };
        }

        public async Task<ApiResponse> CreateGlobalServiceAsync(ServiceRequest request, int adminId)
        {
            if (!await _accessControl.IsSuperuserAsync(adminId))
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền tạo dịch vụ hệ thống." };

            var service = new Service
            {
                ServiceName = request.ServiceName,
                ServiceCode = request.ServiceCode,
                Unit = request.Unit,
                DefaultPrice = request.DefaultPrice,
                CalculationType = request.CalculationType,
                IsSystemDefault = true,
                IsActive = true,
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Services.Add(service);
            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Tạo dịch vụ hệ thống thành công.", Data = service };
        }

        public async Task<ApiResponse> UpdateGlobalServiceAsync(int serviceId, decimal defaultPrice, int adminId)
        {
            var service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.IsSystemDefault);
            if (service == null)
                return new ApiResponse { Success = false, Message = "Không tìm thấy dịch vụ." };

            service.DefaultPrice = defaultPrice;
            service.UpdatedAt = DateTime.Now;

            // Update all existing RoomServiceSetting that use this service
            var roomSettings = await _db.RoomServiceSettings.Where(rs => rs.ServiceId == serviceId).ToListAsync();
            foreach(var rs in roomSettings)
            {
                rs.UnitPrice = defaultPrice;
                rs.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            // Gửi thông báo cho tất cả khách thuê
            string priceFormatted = defaultPrice.ToString("N0") + "đ";
            await _notificationService.CreateNotificationForRolesAsync(
                new[] { "tenant" }, 
                "Cập nhật đơn giá dịch vụ", 
                $"Đơn giá {service.ServiceName} đã được cập nhật mới là {priceFormatted}. Mức giá này sẽ áp dụng cho kỳ hóa đơn tiếp theo.",
                "warning"
            );
            return new ApiResponse { Success = true, Message = "Đã cập nhật phí dịch vụ và gửi thông báo đến khách thuê thành công." };
        }

        public async Task<ApiResponse> SeedDefaultServicesAsync(int adminId)
        {
            if (!await _accessControl.IsSuperuserAsync(adminId))
                return new ApiResponse { Success = false, Message = "Chỉ Superuser mới có quyền khởi tạo dịch vụ mẫu hệ thống." };

            var defaults = new List<Service>
            {
                new Service { ServiceName = "Tiền Điện", ServiceCode = "DIEN", Unit = "kWh", DefaultPrice = 3500, CalculationType = "metered", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now },
                new Service { ServiceName = "Tiền Nước", ServiceCode = "NUOC", Unit = "Khối", DefaultPrice = 25000, CalculationType = "metered", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now },
                new Service { ServiceName = "Phí Rác & Vệ sinh", ServiceCode = "RAC", Unit = "Phòng", DefaultPrice = 50000, CalculationType = "fixed", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now },
                new Service { ServiceName = "Internet / Wifi", ServiceCode = "WIFI", Unit = "Phòng", DefaultPrice = 100000, CalculationType = "fixed", IsSystemDefault = true, IsActive = true, CreatedBy = adminId, CreatedAt = DateTime.Now }
            };

            foreach (var s in defaults)
            {
                if (!await _db.Services.AnyAsync(x => x.ServiceCode == s.ServiceCode && x.IsSystemDefault))
                {
                    _db.Services.Add(s);
                }
            }

            await _db.SaveChangesAsync();
            return new ApiResponse { Success = true, Message = "Đã khởi tạo các dịch vụ mẫu thành công (Điện 3.500, Nước 25.000...)." };
        }
    }
}
