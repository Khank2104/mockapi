using UserManagementSystem.Services;
using UserManagementSystem.Hubs;
using UserManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/quantro-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("=== QuanTro Application Starting ===");

    // Add services to the container.
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHttpClient();
    builder.Services.AddSignalR();
    builder.Services.AddMemoryCache();

    // Add Services
    builder.Services.AddTransient<IEmailService, EmailService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IOtpService, OtpService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<IMotelService, MotelService>();
    builder.Services.AddScoped<IOccupancyService, OccupancyService>();
    builder.Services.AddScoped<IMeterReadingService, MeterReadingService>();
    builder.Services.AddScoped<IInvoiceCalculationService, InvoiceCalculationService>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<IRequestService, RequestService>();
    
    // Add Background Services
    builder.Services.AddHostedService<UserManagementSystem.Services.BackgroundTasks.ContractExpirationService>();

    // Add Swagger/OpenAPI Documentation
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "QuanTro API - Motel Management System",
            Version = "v1.0",
            Description = "Comprehensive REST API for motel property management",
            Contact = new OpenApiContact
            {
                Name = "Development Team",
                Email = "dev@quantro.com"
            }
        });

        // Add JWT Authentication support in Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });

        // Include XML comments for API documentation
        var xmlFile = Path.Combine(AppContext.BaseDirectory, "UserManagementSystem.xml");
        if (File.Exists(xmlFile))
        {
            options.IncludeXmlComments(xmlFile);
        }
    });

    // Configure JWT Authentication
    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["Secret"] ?? "SuperSecretKey123!";
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // CỰC KỲ QUAN TRỌNG: Tắt hoàn toàn việc tự động đổi tên claim
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = "role", 
            NameClaimType = "id"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("X-Access-Token"))
                {
                    context.Token = context.Request.Cookies["X-Access-Token"];
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Nếu không phải yêu cầu API (là yêu cầu trang web), chuyển hướng về Login
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    context.HandleResponse();
                    context.Response.Redirect("/Account/Login");
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
                Log.Information("JWT Token validated for user: {User}. Claims: {Claims}", 
                    context.Principal?.Identity?.Name, string.Join(", ", claims ?? Array.Empty<string>()));
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("SuperuserOnly", policy => 
            policy.RequireAssertion(context => 
                context.User.HasClaim(c => c.Type == "role" && c.Value.ToLower() == "superuser")));

        options.AddPolicy("Management", policy => 
            policy.RequireAssertion(context => 
                context.User.HasClaim(c => c.Type == "role" && 
                    (c.Value.ToLower() == "superuser" || c.Value.ToLower() == "admin"))));

        options.AddPolicy("TenantOnly", policy => 
            policy.RequireAssertion(context => 
                context.User.HasClaim(c => c.Type == "role" && c.Value.ToLower() == "tenant")));
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        // Enable Swagger in Development
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuanTro API v1.0");
            c.RoutePrefix = "swagger"; // Access at /swagger
            c.DefaultModelsExpandDepth(2);
            c.DefaultModelExpandDepth(2);
        });
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication(); 
    app.UseAuthorization();

    // Auth Debugging Middleware
    app.Use(async (context, next) =>
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var claims = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
            Log.Information("Authenticated Request: {Method} {Path} | User: {Name} | Claims: {Claims}", 
                context.Request.Method, context.Request.Path, user.Identity.Name, claims);
        }
        else
        {
            Log.Information("Unauthenticated Request: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
        await next();
    });

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapHub<NotificationHub>("/notificationHub");

    Log.Information("=== QuanTro Application Started Successfully ===");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "=== Application terminated unexpectedly ===");
}
finally
{
    Log.CloseAndFlush();
}

