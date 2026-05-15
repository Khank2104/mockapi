using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public class ExportService : IExportService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAccessControlService _accessControl;

        public ExportService(ApplicationDbContext db, IAccessControlService accessControl)
        {
            _db = db;
            _accessControl = accessControl;
        }

        public async Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId, int requesterId)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Room).ThenInclude(r => r.Motel)
                .Include(i => i.Details)
                .Include(i => i.PrimaryTenant)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null || !await _accessControl.CanAccessRoomAsync(invoice.RoomId, requesterId))
                return Array.Empty<byte>();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("HoaDon");
                
                var title = worksheet.Cell(1, 1);
                title.Value = "HÓA ĐƠN TIỀN TRỌ";
                title.Style.Font.Bold = true;
                title.Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 4).Merge();
                title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(3, 1).Value = "Phòng:";
                worksheet.Cell(3, 2).Value = invoice.Room.RoomCode;
                worksheet.Cell(3, 3).Value = "Khu trọ:";
                worksheet.Cell(3, 4).Value = invoice.Room.Motel.MotelName;

                worksheet.Cell(4, 1).Value = "Khách thuê:";
                worksheet.Cell(4, 2).Value = invoice.PrimaryTenant?.FullName ?? "N/A";
                worksheet.Cell(4, 3).Value = "Tháng/Năm:";
                worksheet.Cell(4, 4).Value = $"{invoice.BillingMonth}/{invoice.BillingYear}";

                var currentRow = 6;
                worksheet.Cell(currentRow, 1).Value = "Nội dung";
                worksheet.Cell(currentRow, 2).Value = "Mô tả";
                worksheet.Cell(currentRow, 3).Value = "Đơn giá";
                worksheet.Cell(currentRow, 4).Value = "Thành tiền";
                
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                currentRow++;
                foreach (var detail in invoice.Details)
                {
                    worksheet.Cell(currentRow, 1).Value = detail.ItemName;
                    worksheet.Cell(currentRow, 2).Value = detail.Note;
                    worksheet.Cell(currentRow, 3).Value = detail.UnitPrice;
                    worksheet.Cell(currentRow, 4).Value = detail.Amount;
                    currentRow++;
                }

                currentRow++;
                var totalCellLabel = worksheet.Cell(currentRow, 3);
                totalCellLabel.Value = "TỔNG CỘNG:";
                totalCellLabel.Style.Font.Bold = true;
                
                var totalCellValue = worksheet.Cell(currentRow, 4);
                totalCellValue.Value = invoice.TotalAmount;
                totalCellValue.Style.Font.Bold = true;
                totalCellValue.Style.Font.FontColor = XLColor.Red;

                worksheet.Range(6, 1, currentRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(6, 1, currentRow, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
