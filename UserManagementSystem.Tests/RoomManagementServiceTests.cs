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
    public class RoomManagementServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;
        private readonly Mock<IAccessControlService> _accessControlMock;
        private readonly RoomManagementService _service;

        public RoomManagementServiceTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();

            _accessControlMock = new Mock<IAccessControlService>();
            _service = new RoomManagementService(_db, _accessControlMock.Object);
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
        public async Task GetRoomServicesAsync_ShouldReturnCorrectActiveStatus()
        {
            // Arrange
            await SetupBasicData();
            var adminRole = await _db.Roles.FirstAsync(r => r.RoleName == "admin");
            var admin = new User { RoleId = adminRole.RoleId, Username = "admin3", Email = "a3@test.com", PasswordHash = "h" };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = admin.UserId };
            var room = new Room { RoomCode = "201", Motel = motel };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            var serviceActive = new Service { ServiceName = "Active Service", ServiceCode = "ACT", IsActive = true, CalculationType = "fixed", DefaultPrice = 10000 };
            var serviceInactive = new Service { ServiceName = "Inactive Service", ServiceCode = "INA", IsActive = true, CalculationType = "fixed", DefaultPrice = 10000 };
            _db.Services.AddRange(serviceActive, serviceInactive);
            await _db.SaveChangesAsync();

            // Room specific settings
            _db.RoomServiceSettings.Add(new RoomServiceSetting { Room = room, Service = serviceActive, IsActive = true });
            _db.RoomServiceSettings.Add(new RoomServiceSetting { Room = room, Service = serviceInactive, IsActive = false });
            await _db.SaveChangesAsync();

            // Act
            var apiResult = await _service.GetRoomServicesAsync(room.RoomId, admin.UserId);

            // Assert
            Assert.True(apiResult.Success);
            var data = apiResult.Data as IEnumerable<dynamic>;
            Assert.NotNull(data);
            
            var list = data.ToList();
            var activeItem = list.First(s => {
                object obj = (object)s!;
                var prop = obj.GetType().GetProperty("ServiceId");
                return prop != null && object.Equals(prop.GetValue(obj), serviceActive.ServiceId);
            });
            var inactiveItem = list.First(s => {
                object obj = (object)s!;
                var prop = obj.GetType().GetProperty("ServiceId");
                return prop != null && object.Equals(prop.GetValue(obj), serviceInactive.ServiceId);
            });

            var activeProp = activeItem.GetType().GetProperty("IsActive");
            var inactiveProp = inactiveItem.GetType().GetProperty("IsActive");
            Assert.NotNull(activeProp);
            Assert.NotNull(inactiveProp);
            var activeVal = activeProp.GetValue(activeItem);
            var inactiveVal = inactiveProp.GetValue(inactiveItem);
            Assert.NotNull(activeVal);
            Assert.NotNull(inactiveVal);
            Assert.True((bool)activeVal);
            Assert.False((bool)inactiveVal);
        }
    }
}
