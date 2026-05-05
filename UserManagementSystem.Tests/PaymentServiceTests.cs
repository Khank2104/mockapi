using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using UserManagementSystem.Data;
using UserManagementSystem.Models;
using UserManagementSystem.Services;
using Xunit;

namespace UserManagementSystem.Tests
{
    public class PaymentServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly Mock<IAccessControlService> _accessControlMock;
        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();

            _notificationMock = new Mock<INotificationService>();
            _accessControlMock = new Mock<IAccessControlService>();
            _service = new PaymentService(_db, _notificationMock.Object, _accessControlMock.Object);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private async Task SetupBasicData()
        {
            if (!await _db.Roles.AnyAsync(r => r.RoleName == "admin"))
            {
                _db.Roles.Add(new Role { RoleName = "admin" });
                await _db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CreatePayment_ShouldFail_WhenAmountExceedsRemaining()
        {
            // Arrange
            await SetupBasicData();
            var adminRole = await _db.Roles.FirstAsync(r => r.RoleName == "admin");
            var admin = new User { RoleId = adminRole.RoleId, Username = "admin", Email = "a@test.com", PasswordHash = "h" };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = admin.UserId };
            var room = new Room { RoomCode = "101", Motel = motel };
            var tenant = new Tenant { FullName = "Tenant 1", CitizenId = "111" };
            var invoice = new Invoice { TotalAmount = 1000000, InvoiceStatus = "PartiallyPaid", Room = room, PrimaryTenant = tenant, BillingMonth = 5, BillingYear = 2026 };
            _db.Invoices.Add(invoice);
            _db.Payments.Add(new Payment { Invoice = invoice, PaidAmount = 900000, ReceivedBy = admin.UserId });
            await _db.SaveChangesAsync();

            _accessControlMock.Setup(a => a.CanAccessInvoiceAsync(invoice.InvoiceId, admin.UserId)).ReturnsAsync(true);

            var request = new CreatePaymentRequest { InvoiceId = invoice.InvoiceId, Amount = 200000, PaymentDate = DateTime.Now, PaymentMethod = "Cash" };

            // Act
            var result = await _service.CreatePaymentAsync(request, admin.UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("vượt quá", result.Message);
        }

        [Fact]
        public async Task CreatePayment_ShouldSucceed_WhenAmountIsCorrect()
        {
            // Arrange
            await SetupBasicData();
            var adminRole = await _db.Roles.FirstAsync(r => r.RoleName == "admin");
            var admin = new User { RoleId = adminRole.RoleId, Username = "admin2", Email = "a2@test.com", PasswordHash = "h" };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = admin.UserId };
            var room = new Room { RoomCode = "102", Motel = motel };
            var tenant = new Tenant { FullName = "Tenant 2", CitizenId = "222" };
            var invoice = new Invoice { TotalAmount = 1000000, InvoiceStatus = "Unpaid", Room = room, PrimaryTenant = tenant, BillingMonth = 5, BillingYear = 2026 };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            _accessControlMock.Setup(a => a.CanAccessInvoiceAsync(invoice.InvoiceId, admin.UserId)).ReturnsAsync(true);

            var request = new CreatePaymentRequest { InvoiceId = invoice.InvoiceId, Amount = 500000, PaymentDate = DateTime.Now, PaymentMethod = "Cash" };

            // Act
            var result = await _service.CreatePaymentAsync(request, admin.UserId);

            // Assert
            Assert.True(result.Success);
            var updatedInvoice = await _db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == invoice.InvoiceId);
            Assert.NotNull(updatedInvoice);
            Assert.Equal(500000, updatedInvoice.Payments.Sum(p => p.PaidAmount));
            Assert.Equal("PartiallyPaid", updatedInvoice.InvoiceStatus);
        }
    }
}
