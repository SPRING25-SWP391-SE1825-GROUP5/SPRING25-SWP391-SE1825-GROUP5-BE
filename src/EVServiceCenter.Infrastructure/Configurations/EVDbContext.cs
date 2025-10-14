using System;
using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Domain.Configurations;

public partial class EVDbContext : DbContext
{
    public EVDbContext(DbContextOptions<EVDbContext> options) : base(options)
    {
    }

    #region DbSets
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryPart> InventoryParts { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<MaintenanceChecklist> MaintenanceChecklists { get; set; }
    public DbSet<MaintenanceChecklistItem> MaintenanceChecklistItems { get; set; }
    public DbSet<MaintenanceChecklistResult> MaintenanceChecklistResults { get; set; }
    
    public DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageRead> MessageReads { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Otpcode> Otpcodes { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceCenter> ServiceCenters { get; set; }
    public DbSet<ServiceCredit> ServiceCredits { get; set; }
    public DbSet<ServicePart> ServiceParts { get; set; }
    public DbSet<ServiceRequiredSkill> ServiceRequiredSkills { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<TechnicianSkill> TechnicianSkills { get; set; }
    public DbSet<TechnicianTimeSlot> TechnicianTimeSlots { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserPromotion> UserPromotions { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<VehicleModel> VehicleModels { get; set; }
    public DbSet<VehicleModelPart> VehicleModelParts { get; set; }
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<WorkOrderPart> WorkOrderParts { get; set; }
    #endregion

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

        ConfigureBooking(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureInventoryPart(modelBuilder);
        ConfigureInvoice(modelBuilder);
        ConfigureMaintenanceChecklist(modelBuilder);
        ConfigureMaintenanceChecklistItem(modelBuilder);
        
        ConfigureMaintenanceChecklistResult(modelBuilder);
        ConfigureServicePart(modelBuilder);
        ConfigureMaintenanceReminder(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureOtpcode(modelBuilder);
        ConfigurePart(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigurePromotion(modelBuilder);
        ConfigureService(modelBuilder);
        ConfigureServiceCredit(modelBuilder);
        ConfigureServiceCenter(modelBuilder);
        ConfigureStaff(modelBuilder);
        ConfigureSystemSetting(modelBuilder);
        ConfigureTechnician(modelBuilder);
        ConfigureSkill(modelBuilder);
        ConfigureTechnicianSkill(modelBuilder);
        ConfigureTechnicianTimeSlot(modelBuilder);
        ConfigureTimeSlot(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureUserPromotion(modelBuilder);
        ConfigureVehicle(modelBuilder);
        ConfigureWorkOrder(modelBuilder);
        ConfigureWorkOrderPart(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        ConfigureFeedback(modelBuilder);
        ConfigureConversation(modelBuilder);
        ConfigureMessage(modelBuilder);
        ConfigureMessageRead(modelBuilder);
        ConfigureVehicleModel(modelBuilder);
        ConfigureVehicleModelPart(modelBuilder);
        ConfigureServiceRequiredSkill(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    #region Entity Configurations

    private static void ConfigureBooking(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId);
            entity.ToTable("Bookings", "dbo");

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.Property(e => e.SpecialRequests).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(12, 2)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            // Relationships
            entity.HasOne(d => d.Center)
                .WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Slot)
                .WithMany(p => p.Bookings)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Vehicle)
                .WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.ToTable("Customers", "dbo");

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.IsGuest).HasDefaultValue(true);

            entity.HasOne(d => d.User)
                .WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId);
            entity.ToTable("Inventory", "dbo");

            entity.HasIndex(e => e.CenterId).IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.LastUpdated)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Center)
                .WithMany(p => p.Inventories)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureInventoryPart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryPart>(entity =>
        {
            entity.HasKey(e => e.InventoryPartId);
            entity.ToTable("InventoryParts", "dbo");

            entity.HasIndex(e => new { e.InventoryId, e.PartId }).IsUnique();

            entity.Property(e => e.InventoryPartId).HasColumnName("InventoryPartID");
            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.LastUpdated)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Inventory)
                .WithMany(p => p.InventoryParts)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Part)
                .WithMany(p => p.InventoryParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureInvoice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId);
            entity.ToTable("Invoices", "dbo");

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId);

            entity.HasOne(d => d.WorkOrder)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Order)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureMaintenanceChecklist(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceChecklist>(entity =>
        {
            entity.HasKey(e => e.ChecklistId);
            entity.ToTable("MaintenanceChecklists", "dbo");

            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.VehicleModelPartId).HasColumnName("VehicleModelPartID");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.WorkOrder)
                .WithMany(p => p.MaintenanceChecklists)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.VehicleModelPart)
                .WithMany()
                .HasForeignKey(d => d.VehicleModelPartId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    

    private static void ConfigureMaintenanceChecklistItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceChecklistItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.ToTable("MaintenanceChecklistItems", "dbo");

            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);
        });
    }

    private static void ConfigureMaintenanceChecklistResult(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceChecklistResult>(entity =>
        {
            entity.HasKey(e => e.ResultId);
            entity.ToTable("MaintenanceChecklistResults", "dbo");

            entity.Property(e => e.ResultId).HasColumnName("ResultID").ValueGeneratedOnAdd();
            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
            entity.Property(e => e.PartId).HasColumnName("PartID");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Comment).HasMaxLength(250);
            entity.Property(e => e.Result).HasMaxLength(50);

            entity.HasOne(d => d.Checklist)
                .WithMany(p => p.MaintenanceChecklistResults)
                .HasForeignKey(d => d.ChecklistId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Part)
                .WithMany()
                .HasForeignKey(d => d.PartId);
        });
    }

    private static void ConfigureServicePart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServicePart>(entity =>
        {
            entity.HasKey(e => e.ServicePartId);
            entity.ToTable("ServiceParts", "dbo");

            entity.HasIndex(e => new { e.ServiceId, e.PartId }).IsUnique();

            entity.Property(e => e.ServicePartId).HasColumnName("ServicePartID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.PartId).HasColumnName("PartID");

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId);

            entity.HasOne(e => e.Part)
                .WithMany()
                .HasForeignKey(e => e.PartId);
        });
    }

    private static void ConfigureMaintenanceReminder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceReminder>(entity =>
        {
            entity.HasKey(e => e.ReminderId);
            entity.ToTable("MaintenanceReminders", "dbo");

            entity.Property(e => e.ReminderId).HasColumnName("ReminderID");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Vehicle)
                .WithMany(p => p.MaintenanceReminders)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Service)
                .WithMany()
                .HasForeignKey(d => d.ServiceId);
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.ToTable("Notifications", "dbo");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Message).IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ReadAt).HasPrecision(0);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureOtpcode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Otpcode>(entity =>
        {
            entity.HasKey(e => e.Otpid);
            entity.ToTable("OTPCodes", "dbo");

            entity.Property(e => e.Otpid).HasColumnName("OTPID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.Property(e => e.ContactInfo)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Otpcode1)
                .IsRequired()
                .HasMaxLength(6)
                .HasColumnName("OTPCode");
            entity.Property(e => e.Otptype)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("OTPType");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.UsedAt).HasPrecision(0);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Otpcodes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigurePart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartId);
            entity.ToTable("Parts", "dbo");

            entity.HasIndex(e => e.PartNumber).IsUnique();

            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.PartName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PartNumber)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(255);

            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });
    }

    private static void ConfigurePayment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.ToTable("Payments", "dbo", tb => tb.HasTrigger("tr_Payments_DefaultBuyerFromInvoice"));

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PaymentCode).IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PaidByUserId).HasColumnName("PaidByUserId");

            entity.Property(e => e.PaymentCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("PAYOS");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaidAt).HasPrecision(0);

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigurePromotion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions", "dbo");

            entity.HasIndex(e => new { e.StartDate, e.EndDate });
            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate });
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountType)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 2)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });
    }

    private static void ConfigureService(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId);
            entity.ToTable("Services", "dbo");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.ServiceName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });
    }

    private static void ConfigureServiceCredit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceCredit>(entity =>
        {
            entity.HasKey(e => e.CreditId);
            entity.ToTable("ServiceCredits", "dbo");

            entity.Property(e => e.CreditId).HasColumnName("CreditID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.Property(e => e.PriceDiscount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ValidFrom).HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v));
            entity.Property(e => e.ValidTo).HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v));

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.ServiceCredits)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Service)
                .WithMany(p => p.ServiceCredits)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureServiceCenter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceCenter>(entity =>
        {
            entity.HasKey(e => e.CenterId);
            entity.ToTable("ServiceCenters", "dbo");

            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CenterName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });
    }

    private static void ConfigureStaff(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId);
            entity.ToTable("Staff", "dbo");

            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Center)
                .WithMany(p => p.Staff)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureSystemSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey);
            entity.ToTable("SystemSettings", "dbo");

            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.SettingValue).IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });
    }

    private static void ConfigureTechnician(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Technician>(entity =>
        {
            entity.HasKey(e => e.TechnicianId);
            entity.ToTable("Technicians", "dbo");

            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)").HasDefaultValue(null);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Center)
                .WithMany(p => p.Technicians)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Technicians)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureSkill(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId);
            entity.ToTable("Skills", "dbo");

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.SkillId).HasColumnName("SkillID");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });
    }

    private static void ConfigureTechnicianSkill(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TechnicianSkill>(entity =>
        {
            entity.HasKey(e => e.TechnicianSkillId);
            entity.ToTable("TechnicianSkills", "dbo");

            entity.HasIndex(e => new { e.TechnicianId, e.SkillId }).IsUnique();

            entity.Property(e => e.TechnicianSkillId).HasColumnName("TechnicianSkillID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.SkillId).HasColumnName("SkillID");

            entity.HasOne(e => e.Technician)
                .WithMany(t => t.TechnicianSkills)
                .HasForeignKey(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Skill)
                .WithMany(s => s.TechnicianSkills)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTechnicianTimeSlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TechnicianTimeSlot>(entity =>
        {
            entity.HasKey(e => e.TechnicianSlotId);
            entity.ToTable("TechnicianTimeSlots", "dbo");

            entity.HasIndex(e => new { e.WorkDate, e.SlotId });
            entity.HasIndex(e => new { e.TechnicianId, e.WorkDate, e.SlotId }).IsUnique();

            entity.Property(e => e.TechnicianSlotId).HasColumnName("TechnicianSlotID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");

            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.TechnicianTimeSlots)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Slot)
                .WithMany(p => p.TechnicianTimeSlots)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureTimeSlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId);
            entity.ToTable("TimeSlots", "dbo");

            entity.HasIndex(e => e.SlotTime).IsUnique();

            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.SlotLabel)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.SlotTime).HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.ToTable("Users", "dbo");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Gender).HasMaxLength(6);
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });
    }

    private static void ConfigureUserPromotion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPromotion>(entity =>
        {
            entity.ToTable("UserPromotions", "dbo");

            entity.HasIndex(e => new { e.CustomerId, e.UsedAt }).IsDescending(false, true);
            entity.HasIndex(e => new { e.PromotionId, e.UsedAt }).IsDescending(false, true);
            entity.HasIndex(e => new { e.PromotionId, e.BookingId }).IsUnique();
            entity.HasIndex(e => new { e.PromotionId, e.OrderId }).IsUnique();

            entity.Property(e => e.UserPromotionId).HasColumnName("UserPromotionID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("USED");

            entity.Property(e => e.UsedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.UserPromotions)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Promotion)
                .WithMany(p => p.UserPromotions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Booking)
                .WithMany()
                .HasForeignKey(d => d.BookingId);

            entity.HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.Service)
                .WithMany()
                .HasForeignKey(d => d.ServiceId);
        });
    }

    private static void ConfigureVehicle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId);
            entity.ToTable("Vehicles", "dbo");

            entity.HasIndex(e => e.LicensePlate).IsUnique();
            entity.HasIndex(e => e.Vin).IsUnique();

            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.ModelId).HasColumnName("ModelID");

            entity.Property(e => e.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Vin)
                .IsRequired()
                .HasMaxLength(17)
                .HasColumnName("VIN");
            entity.Property(e => e.Color).HasMaxLength(30);

            entity.Property(e => e.PurchaseDate)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    v => v.HasValue ? DateOnly.FromDateTime(v.Value) : (DateOnly?)null)
                .HasColumnName("PurchaseDate");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.VehicleModel)
                .WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureWorkOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.WorkOrderId);
            entity.ToTable("WorkOrders", "dbo");

            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("NOT_STARTED");
            entity.Property(e => e.LicensePlate).HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Booking)
                .WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Technician)
                .WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Vehicle)
                .WithMany()
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Center)
                .WithMany()
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Service)
                .WithMany()
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureWorkOrderPart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderPart>(entity =>
        {
            entity.HasKey(e => new { e.WorkOrderId, e.PartId });
            entity.ToTable("WorkOrderParts", "dbo");

            entity.HasIndex(e => e.VehicleModelPartId);

            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.VehicleModelPartId).HasColumnName("VehicleModelPartID");

            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.WorkOrder)
                .WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Part)
                .WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.VehicleModelPart)
                .WithMany()
                .HasForeignKey(d => d.VehicleModelPartId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.ToTable("Orders", "dbo");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

            entity.Property(e => e.Status).HasDefaultValue("PENDING");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureOrderItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);
            entity.ToTable("OrderItems", "dbo");

            entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");

            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Part)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }

    private static void ConfigureFeedback(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId);
            entity.ToTable("Feedbacks", "dbo");

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");

            entity.Property(e => e.IsAnonymous).HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Order)
                .WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WorkOrder)
                .WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
            
            entity.HasOne(d => d.Part)
                .WithMany()
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Technician)
                .WithMany()
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureConversation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);
            entity.ToTable("Conversations", "dbo");

            entity.HasIndex(e => new { e.Status, e.LastMessageAt });
            entity.HasIndex(e => new { e.CustomerId, e.LastMessageAt });

            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Subject).HasMaxLength(200).HasColumnName("subject");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("OPEN")
                .HasColumnName("status");
            entity.Property(e => e.LastMessageAt)
                .HasColumnName("last_message_at")
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastMessageId).HasColumnName("last_message_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Conversations)
                .HasForeignKey(d => d.CustomerId);
        });
    }

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.ToTable("Messages", "dbo");

            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.HasIndex(e => new { e.SenderUserId, e.CreatedAt });
            entity.HasIndex(e => new { e.SenderCustomerId, e.CreatedAt });
            entity.HasIndex(e => new { e.GuestSessionId, e.CreatedAt });

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.SenderUserId).HasColumnName("sender_user_id");
            entity.Property(e => e.SenderCustomerId).HasColumnName("sender_customer_id");
            entity.Property(e => e.GuestSessionId)
                .HasColumnName("guest_session_id")
                .HasMaxLength(64);
            entity.Property(e => e.SenderDisplayName)
                .HasColumnName("sender_display_name")
                .HasMaxLength(100);
            entity.Property(e => e.SenderContact)
                .HasColumnName("sender_contact")
                .HasMaxLength(100);
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.AttachmentUrl)
                .HasColumnName("attachment_url")
                .HasMaxLength(500);
            entity.Property(e => e.ReplyToMessageId).HasColumnName("reply_to_message_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.SenderUser)
                .WithMany()
                .HasForeignKey(d => d.SenderUserId);

            entity.HasOne(d => d.SenderCustomer)
                .WithMany()
                .HasForeignKey(d => d.SenderCustomerId);

            entity.HasOne(d => d.ReplyToMessage)
                .WithMany()
                .HasForeignKey(d => d.ReplyToMessageId);
        });
    }

    private static void ConfigureMessageRead(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageRead>(entity =>
        {
            entity.HasKey(e => new { e.MessageId, e.UserId });
            entity.ToTable("MessageReads", "dbo");

            entity.HasIndex(e => new { e.UserId, e.MessageId });

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReadAt)
                .HasColumnName("read_at")
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Message)
                .WithMany()
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureVehicleModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.HasKey(e => e.ModelId);
            entity.ToTable("VehicleModel", "dbo");

            entity.HasIndex(e => new { e.ModelName, e.Brand }).IsUnique();

            entity.Property(e => e.ModelId).HasColumnName("ModelID");
            entity.Property(e => e.ModelName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Brand)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
        });
    }

    private static void ConfigureVehicleModelPart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleModelPart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("VehicleModelParts", "dbo");

            entity.HasIndex(e => new { e.ModelId, e.PartId }).IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModelId).HasColumnName("ModelID");
            entity.Property(e => e.PartId).HasColumnName("PartID");

            entity.Property(e => e.IsCompatible).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.VehicleModel)
                .WithMany(p => p.VehicleModelParts)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Part)
                .WithMany(p => p.VehicleModelParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureServiceRequiredSkill(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequiredSkill>(entity =>
        {
            entity.HasKey(e => e.ServiceRequiredSkillId);
            entity.ToTable("ServiceRequiredSkills", "dbo");

            entity.HasIndex(e => new { e.ServiceId, e.SkillId }).IsUnique();

            entity.Property(e => e.ServiceRequiredSkillId).HasColumnName("ServiceRequiredSkillID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.SkillId).HasColumnName("SkillID");

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Skill)
                .WithMany()
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    #endregion

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
