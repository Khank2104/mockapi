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
    public class AccessControlServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;
        private readonly AccessControlService _service;

        public AccessControlServiceTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();

            _service = new AccessControlService(_db);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private async Task SetupBasicData()
        {
            if (!await _db.Roles.AnyAsync(r => r.RoleName == "superuser"))
            {
                _db.Roles.Add(new Role { RoleName = "superuser" });
                _db.Roles.Add(new Role { RoleName = "admin" });
                _db.Roles.Add(new Role { RoleName = "tenant" });
                await _db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CanAccessRoomAsync_ShouldAllowSuperuser()
        {
            // Arrange
            await SetupBasicData();
            var superuserRole = await _db.Roles.FirstAsync(r => r.RoleName == "superuser");
            var superuser = new User { RoleId = superuserRole.RoleId, Username = "superuser", Email = "s@test.com", PasswordHash = "h" };
            _db.Users.Add(superuser);
            await _db.SaveChangesAsync();
            
            var motel = new Motel { MotelName = "Test", OwnerUserId = superuser.UserId };
            var room = new Room { RoomCode = "100", Motel = motel };
            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.CanAccessRoomAsync(room.RoomId, superuser.UserId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanAccessRoomAsync_ShouldBlockTenantFromOtherRooms()
        {
            // Arrange
            await SetupBasicData();
            var tenantRole = await _db.Roles.FirstAsync(r => r.RoleName == "tenant");
            var tenantUser = new User { RoleId = tenantRole.RoleId, Username = "tenant1", Email = "t@test.com", PasswordHash = "h" };
            _db.Users.Add(tenantUser);
            await _db.SaveChangesAsync();
            
            var tenant = new Tenant { UserId = tenantUser.UserId, FullName = "Tenant One", CitizenId = "123" };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            var motel = new Motel { MotelName = "Test", OwnerUserId = tenantUser.UserId }; // Use existing user
            var room1 = new Room { RoomCode = "101", Motel = motel };
            var room2 = new Room { RoomCode = "102", Motel = motel };
            _db.Rooms.AddRange(room1, room2);
            await _db.SaveChangesAsync();

            // Tenant is in room 1
            _db.RoomOccupants.Add(new RoomOccupant { RoomId = room1.RoomId, TenantId = tenant.TenantId, OccupantRole = "Primary", Status = "Staying" });
            await _db.SaveChangesAsync();

            // Act
            var canViewRoom1 = await _service.CanAccessRoomAsync(room1.RoomId, tenantUser.UserId);
            var canViewRoom2 = await _service.CanAccessRoomAsync(room2.RoomId, tenantUser.UserId);

            // Assert
            Assert.True(canViewRoom1);
            Assert.False(canViewRoom2);
        }
    }
}
