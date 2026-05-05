using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class MeterReadingService : IMeterReadingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAccessControlService _accessControl;

        public MeterReadingService(ApplicationDbContext db, IAccessControlService accessControl)
        {
            _db = db;
            _accessControl = accessControl;
        }

        public async Task<ApiResponse> CreateReadingAsync(CreateMeterReadingRequest request, int adminId)
        {
            if (!await _accessControl.CanAccessRoomAsync(request.RoomId, adminId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ hoặc phòng không tồn tại." };

            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user == null || (user.Role.RoleName != "admin" && user.Role.RoleName != "superuser"))
                return new ApiResponse { Success = false, Message = "Chỉ quản trị viên mới có quyền ghi chỉ số." };

            var service = await _db.Services.FindAsync(request.ServiceId);
            if (service == null || service.CalculationType != "metered")
                return new ApiResponse { Success = false, Message = "Dịch vụ không hợp lệ hoặc không hỗ trợ ghi chỉ số." };

            if (await _db.MeterReadings.AnyAsync(r => r.RoomId == request.RoomId && r.ServiceId == request.ServiceId 
                && r.BillingMonth == request.BillingMonth && r.BillingYear == request.BillingYear))
                return new ApiResponse { Success = false, Message = "Chỉ số cho kỳ này đã được ghi nhận." };

            var prevReading = await _db.MeterReadings
                .Where(r => r.RoomId == request.RoomId && r.ServiceId == request.ServiceId)
                .OrderByDescending(r => r.BillingYear)
                .ThenByDescending(r => r.BillingMonth)
                .FirstOrDefaultAsync();

            if (prevReading != null && request.ReadingValue < prevReading.CurrentReading)
                return new ApiResponse { Success = false, Message = $"Chỉ số mới ({request.ReadingValue}) không được nhỏ hơn chỉ số cũ ({prevReading.CurrentReading})." };

            var reading = new MeterReading
            {
                RoomId = request.RoomId,
                ServiceId = request.ServiceId,
                BillingMonth = request.BillingMonth,
                BillingYear = request.BillingYear,
                CurrentReading = request.ReadingValue,
                PreviousReading = prevReading?.CurrentReading ?? 0,
                UsageAmount = request.ReadingValue - (prevReading?.CurrentReading ?? 0),
                RecordedBy = adminId,
                RecordedAt = DateTime.Now,
                Note = request.Note
            };

            _db.MeterReadings.Add(reading);
            await _db.SaveChangesAsync();

            var response = new MeterReadingResponse
            {
                ReadingId = reading.ReadingId,
                RoomId = reading.RoomId,
                ServiceName = service.ServiceName,
                PreviousReading = reading.PreviousReading,
                CurrentReading = reading.CurrentReading,
                UsageAmount = reading.UsageAmount,
                BillingMonth = reading.BillingMonth,
                BillingYear = reading.BillingYear,
                RecordedAt = reading.RecordedAt
            };

            return new ApiResponse { Success = true, Message = "Ghi chỉ số thành công.", Data = response };
        }

        public async Task<ApiResponse> GetReadingsByRoomAsync(int roomId, int month, int year, int requesterId)
        {
            if (!await _accessControl.CanAccessRoomAsync(roomId, requesterId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var readings = await _db.MeterReadings
                .Include(r => r.Service)
                .Where(r => r.RoomId == roomId && r.BillingMonth == month && r.BillingYear == year)
                .Select(r => new MeterReadingResponse
                {
                    ReadingId = r.ReadingId,
                    RoomId = r.RoomId,
                    ServiceName = r.Service.ServiceName,
                    PreviousReading = r.PreviousReading,
                    CurrentReading = r.CurrentReading,
                    UsageAmount = r.UsageAmount,
                    BillingMonth = r.BillingMonth,
                    BillingYear = r.BillingYear,
                    RecordedAt = r.RecordedAt
                })
                .ToListAsync();
            return new ApiResponse { Success = true, Data = readings };
        }

        public async Task<ApiResponse> GetLatestReadingsAsync(int roomId, int requesterId)
        {
            if (!await _accessControl.CanAccessRoomAsync(roomId, requesterId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            // Lấy chỉ số mới nhất của từng dịch vụ (Điện, Nước...) cho phòng này
            var allReadings = await _db.MeterReadings
                .Include(r => r.Service)
                .Where(r => r.RoomId == roomId)
                .ToListAsync();

            var latestReadings = allReadings
                .GroupBy(r => r.ServiceId)
                .Select(g => g.OrderByDescending(r => r.BillingYear)
                             .ThenByDescending(r => r.BillingMonth)
                             .First())
                .Select(r => new MeterReadingResponse
                {
                    ReadingId = r.ReadingId,
                    RoomId = r.RoomId,
                    ServiceName = r.Service.ServiceName,
                    ServiceId = r.ServiceId,
                    PreviousReading = r.PreviousReading,
                    CurrentReading = r.CurrentReading,
                    UsageAmount = r.UsageAmount,
                    BillingMonth = r.BillingMonth,
                    BillingYear = r.BillingYear,
                    RecordedAt = r.RecordedAt
                })
                .ToList();

            return new ApiResponse { Success = true, Data = latestReadings };
        }
    }
}
