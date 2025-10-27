using System;
using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Configurations;

public partial class EVDbContext : DbContext
{
    public EVDbContext(DbContextOptions<EVDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryPart> InventoryParts { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<MaintenanceChecklist> MaintenanceChecklists { get; set; }
    public DbSet<MaintenanceChecklistResult> MaintenanceChecklistResults { get; set; }
    public DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ConversationMember> ConversationMembers { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Otpcode> Otpcodes { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<PartCategory> PartCategories { get; set; }
    public DbSet<PartCategoryMap> PartCategoryMaps { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<ServiceCenter> ServiceCenters { get; set; }
    public DbSet<ServiceChecklistTemplate> ServiceChecklistTemplates { get; set; }
    public DbSet<ServiceChecklistTemplateItem> ServiceChecklistTemplateItems { get; set; }
    public DbSet<ServicePackage> ServicePackages { get; set; }
    public DbSet<CustomerServiceCredit> CustomerServiceCredits { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<TechnicianTimeSlot> TechnicianTimeSlots { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserPromotion> UserPromotions { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<VehicleModel> VehicleModels { get; set; }
    public DbSet<VehicleModelPart> VehicleModelParts { get; set; }
    public DbSet<WorkOrderPart> WorkOrderParts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            throw new InvalidOperationException(
                "DbContext must be configured through dependency injection. Use Program.cs configuration instead of OnConfiguring.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EVDbContext).Assembly);
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
