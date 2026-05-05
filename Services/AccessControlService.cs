using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UserManagementSystem.Data;

namespace UserManagementSystem.Services
{
    public class AccessControlService : IAccessControlService
    {
        private readonly ApplicationDbContext _db;

        public AccessControlService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsSuperuserAsync(int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Role?.RoleName == "superuser";
        }

        public async Task<bool> IsAdminOfMotelAsync(int adminId, int motelId)
        {
            if (await IsSuperuserAsync(adminId)) return true;
            return await _db.Motels.AnyAsync(m => m.MotelId == motelId && m.OwnerUserId == adminId);
        }

        public async Task<bool> IsAdminOrSuperAsync(int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.Role?.RoleName == "admin" || user?.Role?.RoleName == "superuser";
        }

        public async Task<bool> CanAccessRoomAsync(int roomId, int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;
            if (user.Role.RoleName == "superuser") return true;

            var room = await _db.Rooms.Include(r => r.Motel).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return false;

            if (user.Role.RoleName == "admin")
            {
                return room.Motel.OwnerUserId == userId;
            }

            if (user.Role.RoleName == "tenant")
            {
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tenant == null) return false;
                return await _db.RoomOccupants.AnyAsync(o => o.RoomId == roomId && o.TenantId == tenant.TenantId && o.Status == "Staying");
            }

            return false;
        }

        public async Task<bool> CanAccessInvoiceAsync(int invoiceId, int userId)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;
            if (user.Role.RoleName == "superuser") return true;

            var invoice = await _db.Invoices.Include(i => i.Room).ThenInclude(r => r.Motel).FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice == null) return false;

            if (user.Role.RoleName == "admin")
            {
                return invoice.Room.Motel.OwnerUserId == userId;
            }

            if (user.Role.RoleName == "tenant")
            {
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
                if (tenant == null) return false;
                // Tenant can see payment of their room's invoice
                return await _db.RoomOccupants.AnyAsync(o => o.RoomId == invoice.RoomId && o.TenantId == tenant.TenantId && o.Status == "Staying");
            }

            return false;
        }
    }
}
