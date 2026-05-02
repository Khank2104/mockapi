using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class InvoiceCalculationService : IInvoiceCalculationService
    {
        private readonly ApplicationDbContext _db;

        public InvoiceCalculationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse> CalculateMonthlyInvoiceAsync(int roomId, int month, int year)
        {
            var room = await _db.Rooms
                .Include(r => r.Setting)
                .Include(r => r.Occupants)
                .Include(r => r.ServiceSettings)
                .ThenInclude(ss => ss.Service)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null) return new ApiResponse { Success = false, Message = "Phòng không tồn tại." };

            var contract = await _db.Contracts
                .Where(c => c.RoomId == roomId && c.ContractStatus == "Active" && c.StartDate <= new DateTime(year, month, 1))
                .FirstOrDefaultAsync();

            if (contract == null) return new ApiResponse { Success = false, Message = "Phòng chưa có hợp đồng hiệu lực." };
            if (room.Setting == null) return new ApiResponse { Success = false, Message = "Phòng chưa có cấu hình giá thuê." };

            var details = new List<InvoiceDetailResponse>();
            
            // Metadata for Invoice summary
            decimal roomRent = contract.MonthlyRent;
            int occupantCount = room.Occupants.Count(o => o.Status == "Staying");
            int extraOccupantCount = 0;
            decimal extraOccupantTotal = 0;
            decimal serviceTotal = 0;

            // 1. Tiền phòng cơ bản
            details.Add(new InvoiceDetailResponse
            {
                ServiceName = "Tiền phòng",
                Description = $"Tiền thuê tháng {month}/{year}",
                Quantity = 1,
                UnitPrice = roomRent,
                SubTotal = roomRent
            });

            // 2. Phụ thu người ở
            if (room.Setting.ApplyExtraOccupantFee && occupantCount > room.Setting.StandardOccupants)
            {
                extraOccupantCount = occupantCount - room.Setting.StandardOccupants;
                extraOccupantTotal = extraOccupantCount * room.Setting.ExtraOccupantFee;
                details.Add(new InvoiceDetailResponse
                {
                    ServiceName = "Phụ thu người ở",
                    Description = $"Phụ thu {extraOccupantCount} người vượt định mức",
                    Quantity = extraOccupantCount,
                    UnitPrice = room.Setting.ExtraOccupantFee,
                    SubTotal = extraOccupantTotal
                });
            }

            // 3. Dịch vụ (Luôn sử dụng đơn giá hệ thống)
            var activeServices = await _db.Services.Where(s => s.IsActive).ToListAsync();
            foreach (var service in activeServices)
            {
                decimal currentServiceAmount = 0;
                string description = "";
                double quantity = 1;
                decimal unitPrice = service.DefaultPrice;

                switch (service.CalculationType)
                {
                    case "metered":
                        var reading = await _db.MeterReadings
                            .FirstOrDefaultAsync(r => r.RoomId == roomId && r.ServiceId == service.ServiceId && r.BillingMonth == month && r.BillingYear == year);
                        
                        if (reading != null)
                        {
                            quantity = reading.CurrentReading - reading.PreviousReading;
                            currentServiceAmount = (decimal)quantity * unitPrice;
                            description = $"Chỉ số: {reading.PreviousReading} -> {reading.CurrentReading}";
                        }
                        else
                        {
                            // Đối với điện nước, nếu không có số thì báo lỗi để admin ghi
                            return new ApiResponse { Success = false, Message = $"Thiếu chỉ số dịch vụ '{service.ServiceName}' cho tháng {month}/{year}. Vui lòng nhập chỉ số trước khi chốt hóa đơn." };
                        }
                        break;

                    case "per_person":
                        quantity = occupantCount;
                        currentServiceAmount = (decimal)quantity * unitPrice;
                        description = $"{occupantCount} người";
                        break;

                    case "per_room":
                    case "fixed":
                    default:
                        quantity = 1;
                        currentServiceAmount = unitPrice;
                        description = "Phí cố định";
                        break;
                }

                if (currentServiceAmount > 0)
                {
                    details.Add(new InvoiceDetailResponse
                    {
                        ServiceName = service.ServiceName,
                        Description = description,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        SubTotal = currentServiceAmount
                    });
                    serviceTotal += currentServiceAmount;
                }
            }

            decimal totalAmount = roomRent + extraOccupantTotal + serviceTotal;

            return new ApiResponse
            {
                Success = true,
                Data = new { 
                    Details = details, 
                    TotalAmount = totalAmount,
                    RoomRent = roomRent,
                    OccupantCount = occupantCount,
                    ExtraOccupantCount = extraOccupantCount,
                    ExtraOccupantTotal = extraOccupantTotal,
                    ServiceTotal = serviceTotal
                }
            };
        }
    }
}
