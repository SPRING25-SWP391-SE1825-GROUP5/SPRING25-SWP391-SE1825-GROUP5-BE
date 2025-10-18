// ============================================================================
// EVServiceCenter API - Entry Point
// ============================================================================
// Main configuration file for the EVServiceCenter API application
// This file contains all service registrations, middleware configurations,
// and application startup logic
// ============================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Api.HostedServices;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Repositories;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Threading.Tasks;
using EVServiceCenter.Api.Extensions;
using EVServiceCenter.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using System.IO;
using EVServiceCenter.Application.Configurations;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.SqlServer;
using EVServiceCenter.Api;


// ============================================================================
// APPLICATION BUILDER CONFIGURATION
// ============================================================================
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// ============================================================================
// DATABASE CONFIGURATION
// ============================================================================

builder.Services.AddDbContext<EVDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================================
// CORE SERVICES REGISTRATION
// ============================================================================
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.Configure<BookingRealtimeOptions>(builder.Configuration.GetSection("BookingRealtime"));
// Cache configuration
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<PaymentService>();
builder.Services.Configure<PayOsOptions>(builder.Configuration.GetSection("PayOS"));
builder.Services.Configure<GuestSessionOptions>(builder.Configuration.GetSection("GuestSession"));
builder.Services.Configure<PromotionOptions>(builder.Configuration.GetSection("Promotion"));
builder.Services.Configure<MaintenanceReminderOptions>(builder.Configuration.GetSection("MaintenanceReminder"));
builder.Services.AddSingleton<EVServiceCenter.Application.Interfaces.IHoldStore, EVServiceCenter.Application.Service.InMemoryHoldStore>();
builder.Services.AddScoped<ISettingsService, EVServiceCenter.Application.Service.SettingsService>();
builder.Services.AddScoped<EVServiceCenter.Domain.Interfaces.ISystemSettingRepository, EVServiceCenter.Infrastructure.Repositories.SystemSettingRepository>();



// ============================================================================
// APPLICATION SERVICES REGISTRATION
// ============================================================================

// Authentication & Authorization Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ILoginLockoutService, LoginLockoutService>();

// Communication Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateRenderer, FileEmailTemplateRenderer>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Business Services
builder.Services.AddScoped<ICenterService, CenterService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingHistoryService, BookingHistoryService>();
builder.Services.AddScoped<IOrderHistoryService, OrderHistoryService>();
builder.Services.AddScoped<IGuestBookingService, GuestBookingService>();
// Removed: MaintenancePolicyService no longer used
// Removed: IMaintenanceChecklistItemService
// Note: ChecklistPartService may be deprecated if not needed without ServiceParts
// Payment service removed from DI per requirement
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();
builder.Services.AddScoped<ITechnicianTimeSlotService, TechnicianTimeSlotService>();
// WorkOrderService removed - functionality merged into BookingService

// E-commerce services
builder.Services.AddScoped<IOrderService, OrderService>();
// Wishlist removed
// removed: ProductReviewService deprecated

// Service Package & Credit Services
builder.Services.AddScoped<IServicePackageService, ServicePackageService>();
builder.Services.AddScoped<ICustomerServiceCreditService, CustomerServiceCreditService>();

// Vehicle Model Services
builder.Services.AddScoped<IVehicleModelService, VehicleModelService>();
builder.Services.AddScoped<IVehicleModelPartService, VehicleModelPartService>();

// Chat Services
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// ============================================================================
// REPOSITORY REGISTRATION
// ============================================================================

// Authentication & User Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
// IOtpRepository consolidated into IOtpCodeRepository
builder.Services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();

// Business Logic Repositories
builder.Services.AddScoped<ICenterRepository, CenterRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<ITimeSlotRepository, TimeSlotRepository>();
builder.Services.AddScoped<ITechnicianRepository, TechnicianRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPartRepository, PartRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
// WorkOrderRepository removed - functionality merged into BookingRepository
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
// Removed: MaintenancePolicyRepository no longer used
// Removed: IServicePartRepository registration (ServiceParts deprecated)
builder.Services.AddScoped<IWorkOrderPartRepository, WorkOrderPartRepository>();
builder.Services.AddScoped<IMaintenanceChecklistRepository, MaintenanceChecklistRepository>();
// Removed: IMaintenanceChecklistItemRepository
builder.Services.AddScoped<IMaintenanceChecklistResultRepository, MaintenanceChecklistResultRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
// IOtpCodeRepository already registered above
builder.Services.AddScoped<ITechnicianTimeSlotRepository, TechnicianTimeSlotRepository>();

// Vehicle Model Repositories
builder.Services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();
builder.Services.AddScoped<IVehicleModelPartRepository, VehicleModelPartRepository>();

// Service Package & Credit Repositories
builder.Services.AddScoped<IServicePackageRepository, ServicePackageRepository>();
builder.Services.AddScoped<ICustomerServiceCreditRepository, CustomerServiceCreditRepository>();

// E-commerce repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
// Wishlist repository removed
// removed: ProductReviewRepository deprecated

// Chat repositories
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IConversationMemberRepository, ConversationMemberRepository>();
builder.Services.AddHostedService<BookingPendingCancellationService>();
builder.Services.AddHostedService<PromotionAppliedCleanupService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty)),
        ClockSkew = TimeSpan.Zero // Không cho phép sai lệch thời gian
    };

    // Custom JWT error handling events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại.",
                error = "TOKEN_EXPIRED",
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            return context.Response.WriteAsync(jsonResponse);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Bạn không có quyền truy cập tài nguyên này.",
                error = "FORBIDDEN",
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            return context.Response.WriteAsync(jsonResponse);
        }
    };
});


// ============================================================================
// AUTHORIZATION POLICIES
// ============================================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("ADMIN", "STAFF"));
    options.AddPolicy("TechnicianOrAdmin", policy => policy.RequireRole("ADMIN", "TECHNICIAN"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});


// ============================================================================
// CORS CONFIGURATION
// ============================================================================


// Thêm CORS policy cụ thể cho ASP.NET Core
builder.Services.AddCors(options =>
{
    // Default policy - Allow all origins (chỉ dành cho Development)
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });


    // Policy cho phép tất cả (Alternative cho Development)

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Cấu hình cụ thể cho Production (Recommended)
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:3000",    // React dev server
                  "http://localhost:5173",    // Vite dev server
                  "https://localhost:3000",   // HTTPS localhost
                  "https://localhost:5173",   // HTTPS Vite dev server
                  "https://your-frontend-domain.com" // Production domain
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // Quan trọng cho JWT/Authentication
              .SetIsOriginAllowedToAllowWildcardSubdomains(); // Hỗ trợ Google OAuth
    });

    // Policy chỉ cho localhost (Development với credentials)
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // Policy bảo mật cao cho Production
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("https://your-production-domain.com")
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
              .AllowCredentials();
    });
});



// ============================================================================
// API CONTROLLERS & SWAGGER CONFIGURATION
// ============================================================================

// Controllers
builder.Services.AddControllers();


// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EVServiceCenter API",
        Version = "v1",
        Description = "EV Service Center Management API - Comprehensive solution for electric vehicle service management"
    });

    // Thêm JWT Authentication vào Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Nhập token của bạn (không cần thêm 'Bearer ' - hệ thống sẽ tự động thêm)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
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
            new List<string>()
        }
    });
});


// ============================================================================
// APPLICATION BUILD & CONFIGURATION
// ============================================================================
var app = builder.Build();

// Detailed errors and developer exception page for easier debugging in Development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// ============================================================================
// PORT CONFIGURATION (Production/Deployment)
// ============================================================================
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://*:{port}");
}

// Forwarded headers để chạy đúng sau proxy (Render)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ============================================================================
// MIDDLEWARE PIPELINE CONFIGURATION
// ============================================================================
// Swagger UI - API Documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EVServiceCenter API V1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "EVServiceCenter API Documentation";
});

app.UseCors("AllowSpecificOrigins"); // Use specific policy for better Google OAuth support

// Enable HTTPS redirect for security
app.UseHttpsRedirection();

app.UseStaticFiles();


// Global Exception Handling - MUST be first
app.UseGlobalExceptionHandling();

// JSON Error Handling - for model binding errors
app.UseJsonErrorHandling();

// Guest session cookie middleware
app.UseMiddleware<EVServiceCenter.Api.Middleware.GuestSessionMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health endpoints cho Render
// removed public health and root endpoints per request

app.MapControllers();
app.MapHub<EVServiceCenter.Api.BookingHub>("/hubs/booking");


app.Run();
