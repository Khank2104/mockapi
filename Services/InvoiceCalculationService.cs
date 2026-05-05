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

            // Lấy danh sách người đang ở thực tế
            var currentOccupants = room.Occupants.Where(o => o.Status == "Staying").ToList();
            if (!currentOccupants.Any()) 
                return new ApiResponse { Success = false, Message = "Phòng hiện đang trống, không có người ở để tính tiền." };

            // Tìm hợp đồng để tham chiếu
            var contract = await _db.Contracts
                .Where(c => c.RoomId == roomId && c.ContractStatus == "Active")
                .FirstOrDefaultAsync();

            if (room.Setting == null && contract == null) 
                return new ApiResponse { Success = false, Message = "Phòng chưa được cấu hình giá thuê và cũng không có hợp đồng đang hoạt động." };

            var details = new List<InvoiceDetailResponse>();
            
            // Dữ liệu tính toán dựa trên Hợp đồng (ưu tiên) hoặc Cấu hình phòng
            decimal roomRent = contract?.MonthlyRent ?? room.Setting?.BaseRent ?? 0;
            int occupantCount = currentOccupants.Count;
            int extraOccupantCount = 0;
            decimal extraOccupantTotal = 0;
            decimal serviceTotal = 0;

            // Logic tính tiền phòng linh hoạt (Prorated Rent) dựa trên ngày vào ở
            decimal actualRoomRent = roomRent;
            string rentDescription = $"Tiền thuê tháng {month}/{year}";

            if (contract != null && contract.StartDate.Month == month && contract.StartDate.Year == year)
            {
                int daysInMonth = DateTime.DaysInMonth(year, month);
                int daysStayed = daysInMonth - contract.StartDate.Day + 1;

                if (daysStayed <= 7)
                {
                    actualRoomRent = 0;
                    rentDescription += $" (Ở {daysStayed} ngày - Miễn phí tiền phòng)";
                }
                else if (daysStayed <= 15)
                {
                    actualRoomRent = roomRent / 2;
                    rentDescription += $" (Ở {daysStayed} ngày - Tính 50% tiền phòng)";
                }
                else
                {
                    rentDescription += $" (Ở {daysStayed} ngày - Tính 100% tiền phòng)";
                }
            }

            // 1. Tiền phòng cơ bản
            details.Add(new InvoiceDetailResponse
            {
                ServiceName = "Tiền phòng",
                Description = rentDescription,
                Quantity = 1,
                UnitPrice = actualRoomRent,
                SubTotal = actualRoomRent
            });

            // 2. Phụ thu người ở
            bool applyExtra = room.Setting?.ApplyExtraOccupantFee ?? false;
            int standardOccupants = room.Setting?.StandardOccupants ?? 2;
            decimal extraFee = room.Setting?.ExtraOccupantFee ?? 0;

            if (applyExtra && occupantCount > standardOccupants)
            {
                extraOccupantCount = occupantCount - standardOccupants;
                extraOccupantTotal = extraOccupantCount * extraFee;
                details.Add(new InvoiceDetailResponse
                {
                    ServiceName = "Phụ thu người ở",
                    Description = $"Phụ thu {extraOccupantCount} người vượt định mức",
                    Quantity = extraOccupantCount,
                    UnitPrice = extraFee,
                    SubTotal = extraOccupantTotal
                });
            }

            // 3. Dịch vụ
            var activeServices = await _db.Services.Where(s => s.IsActive).ToListAsync();
            var roomServices = room.ServiceSettings?.ToList() ?? new List<RoomServiceSetting>();
            var activeRoomServiceIds = roomServices.Where(ss => ss.IsActive).Select(ss => ss.ServiceId).ToList();
            
            // Lấy các chỉ số đã ghi trong tháng (nếu có thì bắt buộc phải tính tiền dịch vụ đó)
            var currentMonthReadings = await _db.MeterReadings
                .Where(r => r.RoomId == roomId && r.BillingMonth == month && r.BillingYear == year)
                .ToListAsync();
            var meteredServiceIds = currentMonthReadings.Select(r => r.ServiceId).ToList();

            foreach (var service in activeServices)
            {
                Console.WriteLine($"Checking service: {service.ServiceName} (ID: {service.ServiceId})");
                
                // Bỏ qua nếu phòng không cấu hình sử dụng dịch vụ này, NGOẠI TRỪ trường hợp dịch vụ đó đã được ghi chỉ số trong tháng
                if (!activeRoomServiceIds.Contains(service.ServiceId) && !meteredServiceIds.Contains(service.ServiceId))
                {
                    Console.WriteLine($" => Skipped: Not in activeRoomServiceIds and not in meteredServiceIds.");
                    continue;
                }

                decimal currentServiceAmount = 0;
                string description = "";
                double quantity = 1;

                // Lấy đơn giá: Ưu tiên giá đã cấu hình cho phòng, nếu không có thì lấy giá gốc
                var roomServiceConfig = roomServices.FirstOrDefault(ss => ss.ServiceId == service.ServiceId);
                decimal unitPrice = roomServiceConfig?.UnitPrice ?? service.DefaultPrice;
                Console.WriteLine($" => UnitPrice: {unitPrice}, CalculationType: {service.CalculationType}");

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
                            // Lấy chỉ số mới nhất trước tháng này để làm số cũ nếu chưa có bản ghi đọc số cho tháng này
                            var lastReading = await _db.MeterReadings
                                .Where(r => r.RoomId == roomId && r.ServiceId == service.ServiceId && (r.BillingYear < year || (r.BillingYear == year && r.BillingMonth < month)))
                                .OrderByDescending(r => r.BillingYear).ThenByDescending(r => r.BillingMonth)
                                .FirstOrDefaultAsync();
                            
                            return new ApiResponse { 
                                Success = false, 
                                Message = $"Thiếu chỉ số '{service.ServiceName}'. Vui lòng nhập số mới. (Số cũ: {lastReading?.CurrentReading ?? 0})",
                                Data = new { ServiceId = service.ServiceId, PreviousReading = lastReading?.CurrentReading ?? 0 }
                            };
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

                Console.WriteLine($" => Calculated Amount: {currentServiceAmount}");

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
                    Console.WriteLine($" => Added to details! Running total: {serviceTotal}");
                }
                else
                {
                    Console.WriteLine($" => NOT added to details because amount is 0.");
                }
            }

            decimal totalAmount = actualRoomRent + extraOccupantTotal + serviceTotal;

            return new ApiResponse
            {
                Success = true,
                Data = new { 
                    Details = details, 
                    TotalAmount = totalAmount,
                    RoomRent = actualRoomRent,
                    OccupantCount = occupantCount,
                    ExtraOccupantCount = extraOccupantCount,
                    ExtraOccupantTotal = extraOccupantTotal,
                    ServiceTotal = serviceTotal
                }
            };

        }
    }
}
