using UserManagementSystem.Models;
using UserManagementSystem.Data;
using ClosedXML.Excel;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace UserManagementSystem.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInvoiceCalculationService _calcService;
        private readonly INotificationService _notificationService;
        private readonly IAccessControlService _accessControl;
        private readonly IEmailService _emailService;

        public InvoiceService(ApplicationDbContext db, IInvoiceCalculationService calcService, INotificationService notificationService, IAccessControlService accessControl, IEmailService emailService)
        {
            _db = db;
            _calcService = calcService;
            _notificationService = notificationService;
            _accessControl = accessControl;
            _emailService = emailService;
        }

        public async Task<ApiResponse> GenerateInvoiceAsync(GenerateInvoiceRequest request, int adminId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminId);
            if (user == null || (user.Role.RoleName != "admin" && user.Role.RoleName != "superuser"))
                return new ApiResponse { Success = false, Message = "Chỉ quản trị viên mới có quyền phát hành hóa đơn." };

            if (!await _accessControl.CanAccessRoomAsync(request.RoomId, adminId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ hoặc phòng không tồn tại." };

            if (await _db.Invoices.AnyAsync(i => i.RoomId == request.RoomId && i.BillingMonth == request.BillingMonth && i.BillingYear == request.BillingYear))
                return new ApiResponse { Success = false, Message = "Hóa đơn kỳ này đã tồn tại." };

            var calcResult = await _calcService.CalculateMonthlyInvoiceAsync(request.RoomId, request.BillingMonth, request.BillingYear);
            if (!calcResult.Success) return calcResult;

            dynamic calcData = calcResult.Data!;
            List<InvoiceDetailResponse> details = calcData.Details;

            var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.RoomId == request.RoomId && c.ContractStatus == "Active");
            
            // Tìm người đại diện (Primary Tenant)
            int primaryTenantId = 0;
            if (contract != null)
            {
                primaryTenantId = contract.PrimaryTenantId;
            }
            else
            {
                // Nếu không có hợp đồng, lấy người ở đầu tiên hoặc người có vai trò đại diện
                var primaryOccupant = await _db.RoomOccupants
                    .Where(o => o.RoomId == request.RoomId && o.Status == "Staying")
                    .OrderByDescending(o => o.OccupantRole == "Owner" || o.OccupantRole == "Primary")
                    .FirstOrDefaultAsync();
                
                if (primaryOccupant != null) primaryTenantId = primaryOccupant.TenantId;
            }

            if (primaryTenantId == 0)
                return new ApiResponse { Success = false, Message = "Không tìm thấy người đại diện để xuất hóa đơn." };

            var invoice = new Invoice
            {
                RoomId = request.RoomId,
                BillingMonth = request.BillingMonth,
                BillingYear = request.BillingYear,
                RoomRent = calcData.RoomRent,
                OccupantCount = calcData.OccupantCount,
                ExtraOccupantCount = calcData.ExtraOccupantCount,
                ExtraOccupantTotal = calcData.ExtraOccupantTotal,
                ServiceTotal = calcData.ServiceTotal,
                TotalAmount = calcData.TotalAmount,
                InvoiceStatus = "Unpaid",
                DueDate = request.DueDate,
                ContractId = contract?.ContractId,
                PrimaryTenantId = primaryTenantId,
                CreatedBy = adminId,
                CreatedAt = DateTime.Now
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            foreach (var d in details)
            {
                var service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceName == d.ServiceName);
                _db.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ServiceId = service?.ServiceId,
                    ItemName = d.ServiceName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.SubTotal,
                    Note = d.Description
                });
            }

            await _db.SaveChangesAsync();

            // Reset số dư của khách và Đóng các hóa đơn cũ (nếu có nợ chuyển sang)
            var tenant = await _db.Tenants.FindAsync(primaryTenantId);
            if (tenant != null)
            {
                // 1. Xử lý số dư ví
                decimal appliedBalance = calcData.TotalAmount > 0 ? tenant.Balance : (calcData.RoomRent + calcData.ExtraOccupantTotal + calcData.ServiceTotal);
                tenant.Balance -= appliedBalance;

                // 2. Xử lý nợ cũ (Đánh dấu các hóa đơn cũ là đã thu xong bằng cách chuyển sang hóa đơn này)
                var unpaidInvoices = await _db.Invoices
                    .Include(i => i.Payments)
                    .Where(i => i.RoomId == invoice.RoomId && (i.BillingYear < invoice.BillingYear || (i.BillingYear == invoice.BillingYear && i.BillingMonth < invoice.BillingMonth)) && i.InvoiceStatus != "Paid")
                    .ToListAsync();

                foreach (var oldInv in unpaidInvoices)
                {
                    decimal remaining = oldInv.TotalAmount - oldInv.Payments.Sum(p => p.PaidAmount);
                    if (remaining > 0)
                    {
                        _db.Payments.Add(new Payment
                        {
                            InvoiceId = oldInv.InvoiceId,
                            PaidAmount = remaining,
                            PaymentMethod = "System",
                            PaymentDate = DateTime.Now,
                            Note = $"Nợ {remaining:N0}đ đã được chuyển sang hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear}",
                            ReceivedBy = adminId,
                            CreatedAt = DateTime.Now
                        });
                        oldInv.InvoiceStatus = "Paid";
                        oldInv.UpdatedAt = DateTime.Now;
                    }
                }

                tenant.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            // Notify Tenants in the room
            var room = await _db.Rooms.Include(r => r.Occupants).ThenInclude(o => o.Tenant).ThenInclude(t => t.User).FirstOrDefaultAsync(r => r.RoomId == invoice.RoomId);
            if (room != null)
            {
                foreach (var occupant in room.Occupants.Where(o => o.Status == "Staying" && o.Tenant.UserId.HasValue))
                {
                    await _notificationService.CreateNotificationAsync(occupant.Tenant.UserId!.Value, "Hóa đơn mới", $"Hóa đơn tháng {invoice.BillingMonth}/{invoice.BillingYear} đã được phát hành. Số tiền: {invoice.TotalAmount:N0}đ", "info");
                    
                    // Gửi Email nếu có
                    if (occupant.OccupantRole == "Primary" || occupant.OccupantRole == "Owner")
                    {
                        var u = occupant.Tenant.User;
                        if (u != null && !string.IsNullOrEmpty(u.Email))
                        {
                            string subject = $"[QuanTro] Hóa đơn tiền phòng tháng {invoice.BillingMonth}/{invoice.BillingYear}";
                            string body = $@"
                                <h3>Xin chào {occupant.Tenant.FullName},</h3>
                                <p>Hóa đơn tiền phòng tháng <b>{invoice.BillingMonth}/{invoice.BillingYear}</b> của phòng <b>{room.RoomCode}</b> đã được phát hành.</p>
                                <ul>
                                    <li><b>Tổng tiền cần thanh toán:</b> <span style='color:red;font-size:18px;font-weight:bold;'>{invoice.TotalAmount:N0} VNĐ</span></li>
                                    <li><b>Hạn thanh toán:</b> {invoice.DueDate.ToString("dd/MM/yyyy")}</li>
                                </ul>
                                <p>Vui lòng đăng nhập vào website quản lý trọ để xem chi tiết và tiến hành thanh toán an toàn.</p>
                                <p>Trân trọng,<br/>Ban quản lý khu trọ</p>
                            ";
                            try {
                                await _emailService.SendEmailAsync(u.Email, subject, body);
                            } catch (Exception ex) {
                                // Log email fail
                                Console.WriteLine($"Gửi mail thất bại: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return new ApiResponse { Success = true, Message = "Phát hành hóa đơn thành công.", Data = invoice.InvoiceId };
        }

        public async Task<ApiResponse> GetInvoiceByIdAsync(int invoiceId, int requesterId)
        {
            var i = await _db.Invoices
                .Include(i => i.Room)
                .Include(i => i.Details)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (i == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hóa đơn." };

            if (!await _accessControl.CanAccessRoomAsync(i.RoomId, requesterId))
                return new ApiResponse { Success = false, Message = "Bạn không có quyền xem hóa đơn này." };

            var response = new InvoiceResponse
            {
                InvoiceId = i.InvoiceId,
                RoomId = i.RoomId,
                RoomCode = i.Room.RoomCode,
                BillingMonth = i.BillingMonth,
                BillingYear = i.BillingYear,
                RoomRent = i.RoomRent,
                ServiceTotal = i.ServiceTotal,
                ExtraOccupantTotal = i.ExtraOccupantTotal,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.Payments.Sum(p => p.PaidAmount),
                Status = i.InvoiceStatus,
                PaymentProofPath = i.PaymentProofPath,
                DueDate = i.DueDate,
                Details = i.Details.Select(d => new InvoiceDetailResponse
                {
                    ServiceName = d.ItemName,
                    Description = d.Note ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    SubTotal = d.Amount
                }).ToList()
            };

            return new ApiResponse { Success = true, Data = response };
        }

        public async Task<ApiResponse> GetInvoicesByRoomAsync(int roomId, int requesterId)
        {
            if (!await _accessControl.CanAccessRoomAsync(roomId, requesterId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            var invoices = await _db.Invoices
                .Include(i => i.Payments)
                .Where(i => i.RoomId == roomId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            
            var result = invoices.Select(i => new {
                i.InvoiceId,
                i.BillingMonth,
                i.BillingYear,
                i.TotalAmount,
                PaidAmount = i.Payments.Sum(p => p.PaidAmount),
                i.InvoiceStatus,
                i.CreatedAt
            });

            return new ApiResponse { Success = true, Data = result };
        }

        public async Task<ApiResponse> GetInvoicesByTenantAsync(int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy thông tin khách thuê." };

            var roomIds = await _db.RoomOccupants
                .Where(o => o.TenantId == tenant.TenantId && o.Status == "Staying")
                .Select(o => o.RoomId)
                .ToListAsync();

            var invoices = await _db.Invoices
                .Include(i => i.Payments)
                .Where(i => i.PrimaryTenantId == tenant.TenantId || roomIds.Contains(i.RoomId))
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .ToListAsync();

            var result = new {
                balance = tenant.Balance,
                invoices = invoices.Select(i => new {
                    i.InvoiceId,
                    i.BillingMonth,
                    i.BillingYear,
                    i.TotalAmount,
                    PaidAmount = i.Payments.Sum(p => p.PaidAmount),
                    i.InvoiceStatus,
                    i.DueDate,
                    i.CreatedAt
                })
            };

            return new ApiResponse { Success = true, Data = result };
        }

        public async Task<ApiResponse> GetTenantRoomInfoAsync(int tenantUserId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == tenantUserId);
            if (tenant == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hồ sơ khách thuê." };

            var occupant = await _db.RoomOccupants
                .Include(o => o.Room).ThenInclude(r => r.Motel)
                .Include(o => o.Room).ThenInclude(r => r.Setting)
                .FirstOrDefaultAsync(o => o.TenantId == tenant.TenantId && o.Status == "Staying");

            if (occupant == null) return new ApiResponse { Success = false, Message = "Bạn hiện chưa ở phòng nào." };

            var room = occupant.Room;
            
            // Get all staying occupants in the room
            var occupants = await _db.RoomOccupants
                .Include(o => o.Tenant)
                .Where(o => o.RoomId == room.RoomId && o.Status == "Staying")
                .Select(o => new {
                    o.Tenant.FullName,
                    o.OccupantRole,
                    o.Tenant.Phone,
                    o.CheckInDate
                })
                .ToListAsync();

            // Get services assigned to this room
            var services = await _db.RoomServiceSettings
                .Include(rss => rss.Service)
                .Where(rss => rss.RoomId == room.RoomId)
                .Select(rss => new {
                    rss.Service.ServiceName,
                    rss.Service.CalculationType,
                    UnitPrice = rss.UnitPrice
                })
                .ToListAsync();

            return new ApiResponse { 
                Success = true, 
                Data = new {
                    MotelName = room.Motel.MotelName,
                    RoomCode = room.RoomCode,
                    Area = room.Area,
                    MonthlyRent = room.Setting?.BaseRent ?? 0,
                    CheckInDate = occupant.CheckInDate,
                    Occupants = occupants,
                    Services = services
                }
            };
        }

        public async Task<ApiResponse> GetBillingSummaryAsync(int month, int year, int adminId, int? motelId = null, int page = 1, int pageSize = 10)
        {
            var isSuper = await _db.Users.AnyAsync(u => u.UserId == adminId && u.Role.RoleName == "superuser");

            var query = _db.Rooms
                .Include(r => r.Motel)
                .Include(r => r.Occupants)
                .Where(r => (isSuper || r.Motel.OwnerUserId == adminId));

            if (motelId.HasValue && motelId.Value > 0)
            {
                query = query.Where(r => r.MotelId == motelId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var rooms = await query
                .OrderBy(r => r.MotelId).ThenBy(r => r.RoomCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var roomIds = rooms.Select(r => r.RoomId).ToList();

            var readings = await _db.MeterReadings
                .Include(r => r.Service)
                .Where(r => roomIds.Contains(r.RoomId) && r.BillingMonth == month && r.BillingYear == year)
                .ToListAsync();

            var invoices = await _db.Invoices
                .Where(i => roomIds.Contains(i.RoomId) && i.BillingMonth == month && i.BillingYear == year)
                .ToListAsync();

            var summary = rooms.Select(r =>
            {
                var elecReading = readings.FirstOrDefault(rd =>
                    rd.RoomId == r.RoomId && (
                        rd.Service.ServiceCode.ToLower().Contains("electric") ||
                        rd.Service.ServiceCode.ToLower().Contains("elec") ||
                        rd.Service.ServiceCode.ToLower().Contains("dien") ||
                        (rd.Service.CalculationType == "metered" && rd.Service.ServiceName.ToLower().Contains("điện"))
                    ));

                var waterReading = readings.FirstOrDefault(rd =>
                    rd.RoomId == r.RoomId &&
                    rd.ReadingId != (elecReading != null ? elecReading.ReadingId : 0) && (
                        rd.Service.ServiceCode.ToLower().Contains("water") ||
                        rd.Service.ServiceCode.ToLower().Contains("wat") ||
                        rd.Service.ServiceCode.ToLower().Contains("nuoc") ||
                        (rd.Service.CalculationType == "metered" && rd.Service.ServiceName.ToLower().Contains("nước"))
                    ));

                var invoice = invoices.FirstOrDefault(i => i.RoomId == r.RoomId);

                return new
                {
                    roomId = r.RoomId,
                    roomCode = r.RoomCode,
                    motelName = r.Motel.MotelName,
                    isOccupied = r.Occupants.Any(o => o.Status == "Staying"),
                    electricity = elecReading == null ? null : (object)new
                    {
                        readingId = elecReading.ReadingId,
                        serviceId = elecReading.ServiceId,
                        serviceName = elecReading.Service.ServiceName,
                        previousReading = elecReading.PreviousReading,
                        currentReading = elecReading.CurrentReading,
                        usageAmount = elecReading.UsageAmount
                    },
                    water = waterReading == null ? null : (object)new
                    {
                        readingId = waterReading.ReadingId,
                        serviceId = waterReading.ServiceId,
                        serviceName = waterReading.Service.ServiceName,
                        previousReading = waterReading.PreviousReading,
                        currentReading = waterReading.CurrentReading,
                        usageAmount = waterReading.UsageAmount
                    },
                    invoice = invoice == null ? null : (object)new
                    {
                        invoiceId = invoice.InvoiceId,
                        invoiceStatus = invoice.InvoiceStatus,
                        totalAmount = invoice.TotalAmount
                    }
                };
            }).ToList();

            return new ApiResponse 
            { 
                Success = true, 
                Data = new {
                    items = summary,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    currentPage = page,
                    pageSize = pageSize
                }
            };
        }

        public async Task<ApiResponse> GetDashboardFinancialSummaryAsync(int month, int year, int adminId, int? motelId = null)
        {
            var isSuper = await _db.Users.AnyAsync(u => u.UserId == adminId && u.Role.RoleName == "superuser");

            var query = _db.Invoices
                .Include(i => i.Room)
                .ThenInclude(r => r.Motel)
                .Where(i => i.BillingMonth == month && i.BillingYear == year && (isSuper || i.Room.Motel.OwnerUserId == adminId));

            if (motelId.HasValue && motelId.Value > 0)
            {
                query = query.Where(i => i.Room.MotelId == motelId.Value);
            }

            var invoices = await query.Include(i => i.Payments).ToListAsync();

            var totalExpected = invoices.Sum(i => i.TotalAmount);
            var totalCollected = invoices.Sum(i => i.Payments.Sum(p => p.PaidAmount));
            var paidCount = invoices.Count(i => i.InvoiceStatus == "Paid");
            var unpaidCount = invoices.Count(i => i.InvoiceStatus == "Unpaid" || i.InvoiceStatus == "PartiallyPaid" || i.InvoiceStatus == "Pending");
            
            // Get last 5 payments for this period
            var lastPayments = await _db.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Room)
                .Where(p => p.Invoice.BillingMonth == month && p.Invoice.BillingYear == year && (isSuper || p.Invoice.Room.Motel.OwnerUserId == adminId))
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new {
                    roomCode = p.Invoice.Room.RoomCode,
                    amount = p.PaidAmount,
                    date = p.PaymentDate,
                    method = p.PaymentMethod
                })
                .ToListAsync();

            return new ApiResponse
            {
                Success = true,
                Data = new
                {
                    totalExpected,
                    totalCollected,
                    paidCount,
                    unpaidCount,
                    lastPayments
                }
            };
        }

        public async Task<ApiResponse> GetRevenueChartDataAsync(int adminId, int? motelId = null)
        {
            var isSuper = await _db.Users.AnyAsync(u => u.UserId == adminId && u.Role.RoleName == "superuser");

            var query = _db.Invoices
                .Include(i => i.Room)
                .ThenInclude(r => r.Motel)
                .Include(i => i.Payments)
                .Where(i => isSuper || i.Room.Motel.OwnerUserId == adminId);

            if (motelId.HasValue && motelId.Value > 0)
            {
                query = query.Where(i => i.Room.MotelId == motelId.Value);
            }

            // Lấy 6 tháng gần nhất (bao gồm cả tháng hiện tại)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            
            var startDate = DateTime.Now.AddMonths(-5);
            int startMonth = startDate.Month;
            int startYear = startDate.Year;

            var invoices = await query
                .Where(i => i.BillingYear > startYear || (i.BillingYear == startYear && i.BillingMonth >= startMonth))
                .ToListAsync();

            var chartData = new List<object>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                int tMonth = targetDate.Month;
                int tYear = targetDate.Year;

                var monthlyInvoices = invoices.Where(inv => inv.BillingMonth == tMonth && inv.BillingYear == tYear).ToList();
                
                decimal expected = monthlyInvoices.Sum(inv => inv.TotalAmount);
                decimal collected = monthlyInvoices.Sum(inv => inv.Payments.Sum(p => p.PaidAmount));

                chartData.Add(new {
                    month = $"Tháng {tMonth}",
                    expected = expected,
                    collected = collected
                });
            }

            return new ApiResponse { Success = true, Data = chartData };
        }

        public async Task<ApiResponse> DeleteInvoiceAsync(int invoiceId, int adminId)
        {
            var invoice = await _db.Invoices.Include(i => i.Payments).Include(i => i.Details).FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice == null) return new ApiResponse { Success = false, Message = "Không tìm thấy hóa đơn." };

            if (!await _accessControl.CanAccessRoomAsync(invoice.RoomId, adminId))
                return new ApiResponse { Success = false, Message = "Quyền hạn không đủ." };

            if (invoice.InvoiceStatus == "Paid" || invoice.Payments.Any())
                return new ApiResponse { Success = false, Message = "Không thể xóa hóa đơn đã có dữ liệu thanh toán." };

            _db.InvoiceDetails.RemoveRange(invoice.Details);
            _db.Invoices.Remove(invoice);
            await _db.SaveChangesAsync();

            return new ApiResponse { Success = true, Message = "Đã hủy hóa đơn thành công." };
        }
    }
}
