using UserManagementSystem.Data;
using UserManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace UserManagementSystem.Services
{
    /// <summary>
    /// Seeds the database with essential default data on first startup.
    /// Credentials are read from environment variables for security.
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, IConfiguration configuration)
        {
            try
            {
                Log.Information("=== Starting Database Seed ===");

                // --- 1. Seed Roles ---
                await SeedRolesAsync(context);

                // --- 2. Seed Default Superuser ---
                await SeedSuperuserAsync(context, configuration);

                Log.Information("=== Database Seed Completed ===");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during database seeding.");
            }
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            if (await context.Roles.AnyAsync())
            {
                Log.Information("Roles already seeded, skipping.");
                return;
            }

            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Superuser", Description = "System super administrator with full access" },
                new Role { RoleId = 2, RoleName = "Admin",     Description = "Motel owner / manager" },
                new Role { RoleId = 3, RoleName = "Tenant",    Description = "Tenant / room occupant" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            Log.Information("Seeded {Count} roles successfully.", roles.Count);
        }

        private static async Task SeedSuperuserAsync(ApplicationDbContext context, IConfiguration configuration)
        {
            const string superuserUsername = "superadmin";

            if (await context.Users.AnyAsync(u => u.Username == superuserUsername))
            {
                Log.Information("Superuser already exists, skipping seed.");
                return;
            }

            // Read password from environment variable (set on Render dashboard)
            // NEVER hardcode passwords in source code!
            var rawPassword = Environment.GetEnvironmentVariable("SEED_SUPERUSER_PASSWORD")
                              ?? configuration["SeedData:SuperuserPassword"]
                              ?? "QuanTro@Demo2025!";

            if (rawPassword == "QuanTro@Demo2025!")
            {
                Log.Warning("=== SECURITY WARNING: Using default seed password. " +
                            "Set 'SEED_SUPERUSER_PASSWORD' environment variable on Render for production! ===");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);

            var superuser = new User
            {
                Username      = superuserUsername,
                PasswordHash  = hashedPassword,
                Email         = "superadmin@quantro.demo",
                Name          = "Super Administrator",
                Phone         = null,
                RoleId        = 1, // Superuser
                Status        = "Active",
                MustChangePassword = false,
                IsFirstLogin  = false,
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };

            await context.Users.AddAsync(superuser);
            await context.SaveChangesAsync();

            Log.Information("Seeded default Superuser: '{Username}' (Role: Superuser). " +
                            "Password was loaded from environment variable.", superuserUsername);
        }
    }
}
