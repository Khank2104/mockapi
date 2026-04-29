using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Models;

namespace UserManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        
        // Motel Management
        public DbSet<Motel> Motels { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomSetting> RoomSettings { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<RoomServiceSetting> RoomServiceSettings { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<RoomOccupant> RoomOccupants { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Request> Requests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "superuser", Description = "Hệ thống tối cao" },
                new Role { RoleId = 2, RoleName = "admin", Description = "Chủ trọ / Quản lý" },
                new Role { RoleId = 3, RoleName = "tenant", Description = "Khách thuê" }
            );

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Motel Relationships
            modelBuilder.Entity<Motel>()
                .HasOne(m => m.Owner)
                .WithMany()
                .HasForeignKey(m => m.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Floor Relationships
            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Motel)
                .WithMany(m => m.Floors)
                .HasForeignKey(f => f.MotelId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room Relationships
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Motel)
                .WithMany(m => m.Rooms)
                .HasForeignKey(r => r.MotelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.SetNull);

            // RoomSetting - Room (1:1 Fixed for Phase 1)
            modelBuilder.Entity<RoomSetting>()
                .HasOne(rs => rs.Room)
                .WithOne(r => r.Setting)
                .HasForeignKey<RoomSetting>(rs => rs.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomSetting>()
                .HasOne(rs => rs.Creator)
                .WithMany()
                .HasForeignKey(rs => rs.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Service
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // RoomServiceSetting
            modelBuilder.Entity<RoomServiceSetting>()
                .HasOne(rss => rss.Room)
                .WithMany(r => r.ServiceSettings)
                .HasForeignKey(rss => rss.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomServiceSetting>()
                .HasOne(rss => rss.Service)
                .WithMany()
                .HasForeignKey(rss => rss.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomServiceSetting>()
                .HasOne(rss => rss.Creator)
                .WithMany()
                .HasForeignKey(rss => rss.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Tenant>(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // RoomOccupant
            modelBuilder.Entity<RoomOccupant>()
                .HasOne(ro => ro.Room)
                .WithMany(r => r.Occupants)
                .HasForeignKey(ro => ro.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomOccupant>()
                .HasOne(ro => ro.Tenant)
                .WithMany(t => t.RoomOccupancies)
                .HasForeignKey(ro => ro.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contract
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Room)
                .WithMany(r => r.Contracts)
                .HasForeignKey(c => c.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.PrimaryTenant)
                .WithMany()
                .HasForeignKey(c => c.PrimaryTenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // MeterReading
            modelBuilder.Entity<MeterReading>()
                .HasOne(mr => mr.Room)
                .WithMany()
                .HasForeignKey(mr => mr.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MeterReading>()
                .HasOne(mr => mr.Service)
                .WithMany()
                .HasForeignKey(mr => mr.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MeterReading>()
                .HasOne(mr => mr.Recorder)
                .WithMany()
                .HasForeignKey(mr => mr.RecordedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Room)
                .WithMany()
                .HasForeignKey(i => i.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.PrimaryTenant)
                .WithMany()
                .HasForeignKey(i => i.PrimaryTenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Contract)
                .WithMany()
                .HasForeignKey(i => i.ContractId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Creator)
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // InvoiceDetail
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Invoice)
                .WithMany(i => i.Details)
                .HasForeignKey(id => id.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Service)
                .WithMany()
                .HasForeignKey(id => id.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Receiver)
                .WithMany()
                .HasForeignKey(p => p.ReceivedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Request
            modelBuilder.Entity<Request>()
                .HasOne(req => req.Room)
                .WithMany()
                .HasForeignKey(req => req.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Request>()
                .HasOne(req => req.Tenant)
                .WithMany()
                .HasForeignKey(req => req.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Request>()
                .HasOne(req => req.Handler)
                .WithMany()
                .HasForeignKey(req => req.HandledBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
