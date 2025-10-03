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
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Api.HostedServices;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Repositories;
using EVServiceCenter.Domain.IRepositories;
using EVServiceCenter.Domain.Configurations;
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
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<PaymentService>();
builder.Services.Configure<PayOsOptions>(builder.Configuration.GetSection("PayOS"));


// ============================================================================
// APPLICATION SERVICES REGISTRATION
// ============================================================================

// Authentication & Authorization Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Communication Services
builder.Services.AddScoped<IEmailService, EmailService>();
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
builder.Services.AddScoped<GuestBookingService>();
builder.Services.AddScoped<ISkillService, SkillService>();
// Payment service removed from DI per requirement
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();
// CenterScheduleService removed
builder.Services.AddScoped<ITechnicianTimeSlotService, TechnicianTimeSlotService>();

// E-commerce services
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
// Wishlist removed
// removed: ProductReviewService deprecated

// Vehicle Model Services
builder.Services.AddScoped<IVehicleModelService, VehicleModelService>();
builder.Services.AddScoped<IVehicleModelPartService, VehicleModelPartService>();

// ============================================================================
// REPOSITORY REGISTRATION
// ============================================================================

// Authentication & User Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
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
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IMaintenancePolicyRepository, MaintenancePolicyRepository>();
builder.Services.AddScoped<IServicePartRepository, ServicePartRepository>();
builder.Services.AddScoped<IWorkOrderPartRepository, WorkOrderPartRepository>();
builder.Services.AddScoped<IMaintenanceChecklistRepository, MaintenanceChecklistRepository>();
builder.Services.AddScoped<IMaintenanceChecklistResultRepository, MaintenanceChecklistResultRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
// CenterScheduleRepository removed
builder.Services.AddScoped<ITechnicianTimeSlotRepository, TechnicianTimeSlotRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();

// Vehicle Model Repositories
builder.Services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();
builder.Services.AddScoped<IVehicleModelPartRepository, VehicleModelPartRepository>();

// E-commerce repositories
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();
// Wishlist repository removed
// removed: ProductReviewRepository deprecated
builder.Services.AddHostedService<BookingPendingCancellationService>();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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

builder.Services.AddCors(options =>
{
    // Default policy - Allow all origins (Development)
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Allow all policy (Alternative)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Hoặc cấu hình cụ thể cho production
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",    // React dev server
                "http://localhost:5173",    // Vite dev server
                "https://your-frontend-domain.com" // Production domain
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


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

app.UseCors(); // default policy

// Tránh redirect loop sau proxy: chỉ bật HTTPS redirect ở non-production
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthenticationErrorHandling();
app.UseAuthorization();

// Health endpoints cho Render
app.MapGet("/healthz", () => Results.Ok("OK")).WithTags("Health");
app.MapGet("/", () => Results.Ok("EVServiceCenter API"));

app.MapControllers();

app.Run();
