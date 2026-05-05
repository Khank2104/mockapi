using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.Models;
using UserManagementSystem.Services;
using Xunit;

namespace UserManagementSystem.Tests
{
    public class InvoiceCalculationServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;
        private readonly InvoiceCalculationService _service;

        public InvoiceCalculationServiceTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();

            _service = new InvoiceCalculationService(_db);
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
                _db.Roles.Add(new Role { RoleName = "superuser" });
                _db.Roles.Add(new Role { RoleName = "tenant" });
                await _db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CalculateMonthlyInvoice_ShouldUseDefaultPriceFromGlobalService()
        {
            // Arrange
            await SetupBasicData();
            var adminRole = await _db.Roles.FirstAsync(r => r.RoleName == "admin");
            var admin = new User { RoleId = adminRole.RoleId, Username = "admin4", Email = "a4@test.com", PasswordHash = "h" };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = admin.UserId };
            var room = new Room { RoomCode = "101", Motel = motel };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            var roomSetting = new RoomSetting { Room = room, BaseRent = 5000000, StandardOccupants = 2 };
            _db.RoomSettings.Add(roomSetting);

            var tenant = new Tenant { FullName = "Tenant 1", CitizenId = "111" };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            _db.RoomOccupants.Add(new RoomOccupant { Room = room, Tenant = tenant, OccupantRole = "Primary", Status = "Staying" });

            var service = new Service { ServiceName = "Electricity", ServiceCode = "DIEN", Unit = "kWh", DefaultPrice = 3500, CalculationType = "metered", IsActive = true };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            _db.RoomServiceSettings.Add(new RoomServiceSetting { Room = room, Service = service, IsActive = true });
            _db.MeterReadings.Add(new MeterReading { Room = room, Service = service, BillingMonth = 5, BillingYear = 2026, CurrentReading = 100, PreviousReading = 50 });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.CalculateMonthlyInvoiceAsync(room.RoomId, 5, 2026);

            // Assert
            Assert.True(result.Success);
            var data = result.Data;
            Assert.NotNull(data);
            var detailsProperty = data.GetType().GetProperty("Details");
            Assert.NotNull(detailsProperty);
            var detailsValue = detailsProperty.GetValue(data);
            Assert.NotNull(detailsValue);
            var details = (List<InvoiceDetailResponse>)detailsValue;
            var detail = details.First(d => d.ServiceName == "Electricity");
            Assert.Equal(3500, detail.UnitPrice);
            Assert.Equal(50 * 3500, detail.SubTotal);
        }

        [Fact]
        public async Task CalculateMonthlyInvoice_ShouldExcludeDisabledServicesInRoomServiceSettings()
        {
            // Arrange
            await SetupBasicData();
            var adminRole = await _db.Roles.FirstAsync(r => r.RoleName == "admin");
            var admin = new User { RoleId = adminRole.RoleId, Username = "admin5", Email = "a5@test.com", PasswordHash = "h" };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = admin.UserId };
            var room = new Room { RoomCode = "102", Motel = motel };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            var roomSetting = new RoomSetting { Room = room, BaseRent = 5000000, StandardOccupants = 2 };
            _db.RoomSettings.Add(roomSetting);

            var tenant = new Tenant { FullName = "Tenant 2", CitizenId = "222" };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            _db.RoomOccupants.Add(new RoomOccupant { Room = room, Tenant = tenant, OccupantRole = "Primary", Status = "Staying" });

            var service = new Service { ServiceName = "Wifi", ServiceCode = "WIFI", Unit = "Month", DefaultPrice = 100000, CalculationType = "fixed", IsActive = true };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            _db.RoomServiceSettings.Add(new RoomServiceSetting { Room = room, Service = service, IsActive = false });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.CalculateMonthlyInvoiceAsync(room.RoomId, 5, 2026);

            // Assert
            Assert.True(result.Success);
            var data = result.Data;
            Assert.NotNull(data);
            var detailsProperty = data.GetType().GetProperty("Details");
            Assert.NotNull(detailsProperty);
            var detailsValue = detailsProperty.GetValue(data);
            Assert.NotNull(detailsValue);
            var details = (List<InvoiceDetailResponse>)detailsValue;
            Assert.DoesNotContain(details, d => d.ServiceName == "Wifi");
        }

        [Fact]
        public async Task CalculateMonthlyInvoice_ShouldFail_WhenMeteredServiceMissingReading()
        {
            // Arrange
            await SetupBasicData();
            var adminRole = await _db.Roles.FirstAsync(r => r.RoleName == "admin");
            var admin = new User { RoleId = adminRole.RoleId, Username = "admin6", Email = "a6@test.com", PasswordHash = "h" };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = admin.UserId };
            var room = new Room { RoomCode = "103", Motel = motel };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            var roomSetting = new RoomSetting { Room = room, BaseRent = 5000000, StandardOccupants = 2 };
            _db.RoomSettings.Add(roomSetting);

            var tenant = new Tenant { FullName = "Tenant 3", CitizenId = "333" };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            _db.RoomOccupants.Add(new RoomOccupant { Room = room, Tenant = tenant, OccupantRole = "Primary", Status = "Staying" });

            var service = new Service { ServiceName = "Water", ServiceCode = "NUOC", Unit = "m3", DefaultPrice = 20000, CalculationType = "metered", IsActive = true };
            _db.Services.Add(service);
            await _db.SaveChangesAsync();

            _db.RoomServiceSettings.Add(new RoomServiceSetting { Room = room, Service = service, IsActive = true });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.CalculateMonthlyInvoiceAsync(room.RoomId, 5, 2026);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Thiếu chỉ số", result.Message);
        }
    }
}
