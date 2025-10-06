using System;
using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Domain.Configurations;

public partial class EVDbContext : DbContext
{


    public EVDbContext(DbContextOptions<EVDbContext> options)
        : base(options)
    {
    }

    public  DbSet<Booking> Bookings { get; set; }

    // Removed BookingServices DbSet in single-service model

    

    public  DbSet<Customer> Customers { get; set; }

    public  DbSet<Inventory> Inventories { get; set; }

    public  DbSet<InventoryPart> InventoryParts { get; set; }






    public  DbSet<Invoice> Invoices { get; set; }

    
    public  DbSet<ServicePart> ServiceParts { get; set; }



    public  DbSet<MaintenanceChecklist> MaintenanceChecklists { get; set; }

    public  DbSet<MaintenanceChecklistResult> MaintenanceChecklistResults { get; set; }

    public  DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }

    public  DbSet<Notification> Notifications { get; set; }

    public  DbSet<Otpcode> Otpcodes { get; set; }

    public  DbSet<Part> Parts { get; set; }

    public  DbSet<Payment> Payments { get; set; }

    public  DbSet<Promotion> Promotions { get; set; }



    public  DbSet<Service> Services { get; set; }

    public  DbSet<ServiceCenter> ServiceCenters { get; set; }

    public  DbSet<ServiceCredit> ServiceCredits { get; set; }

    

    public  DbSet<Staff> Staff { get; set; }

    public  DbSet<SystemSetting> SystemSettings { get; set; }

    public  DbSet<Technician> Technicians { get; set; }

    public  DbSet<TechnicianTimeSlot> TechnicianTimeSlots { get; set; }
    public  DbSet<Skill> Skills { get; set; }
    public  DbSet<TechnicianSkill> TechnicianSkills { get; set; }
    

    public  DbSet<TimeSlot> TimeSlots { get; set; }

    public  DbSet<User> Users { get; set; }

    public  DbSet<UserPromotion> UserPromotions { get; set; }

    public  DbSet<Vehicle> Vehicles { get; set; }

    public  DbSet<VehicleModel> VehicleModels { get; set; }

    public  DbSet<VehicleModelPart> VehicleModelParts { get; set; }

    public  DbSet<WorkOrder> WorkOrders { get; set; }

    // Proposals removed per requirements

    public  DbSet<WorkOrderPart> WorkOrderParts { get; set; }
    public  DbSet<MaintenancePolicy> MaintenancePolicies { get; set; }

    public  DbSet<ServiceRequiredSkill> ServiceRequiredSkills { get; set; }

    // E-commerce tables

    public  DbSet<Order> Orders { get; set; }

    public  DbSet<OrderItem> OrderItems { get; set; }

    // public  DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }



    public  DbSet<Feedback> Feedbacks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=112.78.2.94,1433;Database=ksf00691_team03;User Id=ksf00691_team03;Password=team03@III;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ksf00691_team03");

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Bookings__73951ACD58E24216");

            entity.ToTable("Bookings", "dbo");

            // BookingCode removed

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            // BookingCode removed
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.SpecialRequests).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            // Severity removed
            // TotalSlots removed in single-slot model
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.HasOne(d => d.Center).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Centers");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Customers");

            entity.HasOne(d => d.Slot).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Slot");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Vehicles");
        });

        // BookingServices removed in single-service model

        // CenterSchedule entity removed

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B8CC15439B");

            entity.ToTable("Customers", "dbo");

            entity.HasIndex(e => e.UserId, "UX_Customers_UserID_NotNull")
                .IsUnique()
                .HasFilter("([UserID] IS NOT NULL)");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.IsGuest).HasDefaultValue(true);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .HasConstraintName("FK_Customers_Users");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__F5FDE6D35C44F64D");

            entity.ToTable("Inventory", "dbo");

            entity.HasIndex(e => e.CenterId, "UQ_Inv_Center").IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.LastUpdated)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Center).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inv_Centers");
        });

        modelBuilder.Entity<InventoryPart>(entity =>
        {
            entity.HasKey(e => e.InventoryPartId).HasName("PK__InventoryParts__F5FDE6D35C44F65E");

            entity.ToTable("InventoryParts", "dbo");

            entity.HasIndex(e => new { e.InventoryId, e.PartId }, "UQ_InvPart_InventoryPart").IsUnique();

            entity.Property(e => e.InventoryPartId).HasColumnName("InventoryPartID");
            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.LastUpdated)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Inventory).WithMany(p => p.InventoryParts)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvPart_Inventory");

            entity.HasOne(d => d.Part).WithMany(p => p.InventoryParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvPart_Parts");
        });





        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAD52C746B3F");

            entity.ToTable("Invoices", "dbo");

            // InvoiceType and NormalizedBillingPhone removed

            // entity.HasIndex(e => e.ParentInvoiceId, "IX_Invoices_Parent"); // Column không tồn tại trong database

            entity.HasIndex(e => e.Status, "IX_Invoices_Status");

            // Removed InvoiceNumber unique index

            // Map primary key column name
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            // Removed InvoiceNumber property mapping
            // InvoiceType and NormalizedBillingPhone removed
            // entity.Property(e => e.ParentInvoiceId).HasColumnName("ParentInvoiceID"); // Column không tồn tại trong database
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT");
            // Removed TotalAmount property mapping
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Invoices_Customers");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_WorkOrders");

            entity.HasOne(d => d.Order).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Orders");
        });

        // InvoiceItems removed; invoices link directly to one OrderItem
        modelBuilder.Entity<Invoice>(entity => { });



        modelBuilder.Entity<MaintenanceChecklist>(entity =>
        {
            entity.HasKey(e => e.ChecklistId).HasName("PK__Maintena__4C1D49BAAA52170A");

            entity.ToTable("MaintenanceChecklists", "dbo");

            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.MaintenanceChecklists)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__WorkO__54B68676");
        });

        modelBuilder.Entity<MaintenancePolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId);
            entity.ToTable("MaintenancePolicies", "dbo");

            entity.Property(e => e.PolicyId).HasColumnName("PolicyID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .HasConstraintName("FK_MaintenancePolicies_Services");
        });

        // MaintenanceChecklistItems removed

        modelBuilder.Entity<MaintenanceChecklistResult>(entity =>
        {
            entity.HasKey(e => e.ResultId);

            entity.ToTable("MaintenanceChecklistResults", "dbo");

            entity.Property(e => e.ResultId).HasColumnName("ResultID").ValueGeneratedOnAdd();
            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
            // StepName removed
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.Comment).HasMaxLength(250);
            entity.Property(e => e.Result).HasMaxLength(50);

            // no index on StepName

            entity.HasOne(d => d.Checklist).WithMany(p => p.MaintenanceChecklistResults)
                .HasForeignKey(d => d.ChecklistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MCR_Checklists");

            entity.HasOne(d => d.Part).WithMany()
                .HasForeignKey(d => d.PartId)
                .HasConstraintName("FK_MCR_Parts");
        });

        // ServiceParts mapping
        modelBuilder.Entity<ServicePart>(entity =>
        {
            entity.HasKey(e => new { e.ServiceId, e.PartId });
            entity.ToTable("ServiceParts", "dbo");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.HasOne(e => e.Service).WithMany()
                .HasForeignKey(e => e.ServiceId)
                .HasConstraintName("FK_SP_Services");
            entity.HasOne(e => e.Part).WithMany()
                .HasForeignKey(e => e.PartId)
                .HasConstraintName("FK_SP_Parts");
        });

        modelBuilder.Entity<MaintenanceReminder>(entity =>
        {
            entity.HasKey(e => e.ReminderId).HasName("PK__Maintena__01A830A779AB1D09");

            entity.ToTable("MaintenanceReminders", "dbo");

            entity.Property(e => e.ReminderId).HasColumnName("ReminderID");
            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.MaintenanceReminders)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MR_Vehicles");

            entity.HasOne(d => d.Service).WithMany()
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_MR_Services");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E32AE77D9ED");

            entity.ToTable("Notifications", "dbo");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.ReadAt).HasPrecision(0);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Noti_Users");
        });

        modelBuilder.Entity<Otpcode>(entity =>
        {
            entity.HasKey(e => e.Otpid).HasName("PK__OTPCodes__5C2EC56253E54938");

            entity.ToTable("OTPCodes", "dbo");

            entity.Property(e => e.Otpid).HasColumnName("OTPID");
            entity.Property(e => e.ContactInfo)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.Otpcode1)
                .IsRequired()
                .HasMaxLength(6)
                .HasColumnName("OTPCode");
            entity.Property(e => e.Otptype)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("OTPType");
            entity.Property(e => e.UsedAt).HasPrecision(0);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Otpcodes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPCodes_Users");
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartId).HasName("PK__Parts__7C3F0D3048808AFE");

            entity.ToTable("Parts", "dbo");

            entity.HasIndex(e => e.PartNumber, "UQ__Parts__025D30D9F2F3C21F").IsUnique();

            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PartName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PartNumber)
                .IsRequired()
                .HasMaxLength(50);
            // Removed Unit
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A58C7B37C95");

            entity.ToTable("Payments", "dbo", tb => tb.HasTrigger("tr_Payments_DefaultBuyerFromInvoice"));

            entity.HasIndex(e => e.CreatedAt, "IX_Payments_Created");

            entity.HasIndex(e => e.Status, "IX_Payments_Status");

            // Removed unique index for PayOSOrderCode

            entity.HasIndex(e => e.PaymentCode, "UQ__Payments__106D3BA8E41F72D2").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            // Removed Buyer info columns
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            // Removed PayOSOrderCode mapping
            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("PAYOS");
            entity.Property(e => e.PaidByUserId).HasColumnName("PaidByUserId");
            entity.Property(e => e.PaymentCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions", "dbo");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "IX_Promotions_ActiveDate").HasFilter("([Status]=N'ACTIVE')");

            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_Promotions_StatusDates");

            entity.HasIndex(e => e.Code, "UQ_Promotions_Code").IsUnique();

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            // ApplyFor removed
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountType)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 2)");
            // PromotionType removed
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });



        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB0EAE5210AEC");

            entity.ToTable("Services", "dbo");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ServiceName)
                .IsRequired()
                .HasMaxLength(100);

        });

        modelBuilder.Entity<ServiceCredit>(entity =>
        {
            entity.HasKey(e => e.CreditId).HasName("PK__ServiceC__A4D4B8F4E9A32E1C");

            entity.ToTable("ServiceCredits", "dbo");

            entity.Property(e => e.CreditId).HasColumnName("CreditID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            // Removed InvoiceID column
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

            entity.HasOne(d => d.Customer).WithMany(p => p.ServiceCredits)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCr__Custo__3F466844");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceCredits)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCr__Servi__403A8C7D");

            // Removed FK to Invoice
        });

        

        modelBuilder.Entity<ServiceCenter>(entity =>
        {
            entity.HasKey(e => e.CenterId).HasName("PK__ServiceC__398FC7D760929C24");

            entity.ToTable("ServiceCenters", "dbo");

            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.CenterName)
                .IsRequired()
                .HasMaxLength(100);
            // City removed
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            // Email removed
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });



        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AAF70CFA06C8");

            entity.ToTable("Staff", "dbo");

            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Center).WithMany(p => p.Staff)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Staff_Centers");

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Staff_Users");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK__SystemSe__01E719AC5928C2F7");

            entity.ToTable("SystemSettings", "dbo");

            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.SettingValue).IsRequired();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Technician>(entity =>
        {
            entity.HasKey(e => e.TechnicianId).HasName("PK__Technici__301F82C180EA1203");

            entity.ToTable("Technicians", "dbo");


            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Position).HasMaxLength(100);
            // TechnicianCode removed
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)").HasDefaultValue(null);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Center).WithMany(p => p.Technicians)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tech_Centers");

            entity.HasOne(d => d.User).WithMany(p => p.Technicians)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tech_Users");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId);
            entity.ToTable("Skills", "dbo");
            entity.Property(e => e.SkillId).HasColumnName("SkillID");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<TechnicianSkill>(entity =>
        {
            entity.HasKey(e => new { e.TechnicianId, e.SkillId });
            entity.ToTable("TechnicianSkills", "dbo");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.SkillId).HasColumnName("SkillID");
            // Removed Level and Years in simplified model; add Notes instead
            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(e => e.Technician)
                .WithMany(t => t.TechnicianSkills)
                .HasForeignKey(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TechSkills_Technicians");

            entity.HasOne(e => e.Skill)
                .WithMany(s => s.TechnicianSkills)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TechSkills_Skills");
        });

        

        modelBuilder.Entity<TechnicianTimeSlot>(entity =>
        {
            entity.HasKey(e => e.TechnicianSlotId).HasName("PK__Technici__8892BB75E5FCFE53");

            entity.ToTable("TechnicianTimeSlots", "dbo");

            entity.HasIndex(e => new { e.WorkDate, e.SlotId }, "IX_TTS_DateSlot");

            entity.HasIndex(e => new { e.TechnicianId, e.WorkDate, e.SlotId }, "UX_TTS_TechDateSlot").IsUnique();

            entity.Property(e => e.TechnicianSlotId).HasColumnName("TechnicianSlotID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");

            entity.HasOne(d => d.Slot).WithMany(p => p.TechnicianTimeSlots)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TTS_Slot");

            entity.HasOne(d => d.Technician).WithMany(p => p.TechnicianTimeSlots)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TTS_Tech");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PK__TimeSlot__0A124A4F5ADA88F1");

            entity.ToTable("TimeSlots", "dbo");

            entity.HasIndex(e => e.SlotTime, "UQ__TimeSlot__488B1607F122A36A").IsUnique();

            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SlotLabel)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.SlotTime).HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC0F533A1C");

            entity.ToTable("Users", "dbo");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105349C971121").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(6);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            // FailedLoginAttempts, LockoutUntil removed
        });

        modelBuilder.Entity<UserPromotion>(entity =>
        {
            entity.ToTable("UserPromotions", "dbo");

            entity.HasIndex(e => new { e.CustomerId, e.UsedAt }, "IX_UserPromotions_Customer").IsDescending(false, true);

            entity.HasIndex(e => new { e.PromotionId, e.UsedAt }, "IX_UserPromotions_Promotion").IsDescending(false, true);

            entity.HasIndex(e => new { e.PromotionId, e.BookingId }, "UQ_UserPromotions_Promo_Booking")
                .IsUnique()
                .HasFilter("([BookingID] IS NOT NULL)");
            entity.HasIndex(e => new { e.PromotionId, e.OrderId }, "UQ_UserPromotions_Promo_Order")
                .IsUnique()
                .HasFilter("([OrderID] IS NOT NULL)");

            entity.Property(e => e.UserPromotionId).HasColumnName("UserPromotionID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("USED");
            entity.Property(e => e.UsedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.UserPromotions)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPromotions_Customers");

            entity.HasOne(d => d.Booking).WithMany()
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_UserPromotions_Bookings");

            entity.HasOne(d => d.Order).WithMany()
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_UserPromotions_Orders");

            entity.HasOne(d => d.Promotion).WithMany(p => p.UserPromotions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPromotions_Promotions");

            entity.HasOne(d => d.Service).WithMany()
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_UserPromotions_Services");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicles__476B54B2FA36B2E5");

            entity.ToTable("Vehicles", "dbo");

            entity.HasIndex(e => e.LicensePlate, "UQ__Vehicles__026BC15C5D38E10E").IsUnique();

            entity.HasIndex(e => e.Vin, "UQ__Vehicles__C5DF234C3C1DD5D3").IsUnique();

            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
            entity.Property(e => e.Color).HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Vin)
                .IsRequired()
                .HasMaxLength(17)
                .HasColumnName("VIN");

            entity.Property(e => e.PurchaseDate)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    v => v.HasValue ? DateOnly.FromDateTime(v.Value) : (DateOnly?)null)
                .HasColumnName("PurchaseDate");

            entity.Property(e => e.ModelId).HasColumnName("ModelID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicles_Customers");

            entity.HasOne(d => d.VehicleModel)
                .WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Vehicles_VehicleModel");
        });




        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.WorkOrderId).HasName("PK__WorkOrde__AE75517563E7EB82");

            entity.ToTable("WorkOrders", "dbo");

            // Removed WorkOrderNumber unique index

            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("NOT_STARTED");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.CurrentMileage);
            entity.Property(e => e.LicensePlate).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            // Removed WorkOrderNumber property mapping

            entity.HasOne(d => d.Booking).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WO_Bookings");

            entity.HasOne(d => d.Technician).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WO_Technicians");

            entity.HasOne(d => d.Customer).WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WO_Customers");

            entity.HasOne(d => d.Vehicle).WithMany()
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WO_Vehicles");

            entity.HasOne(d => d.Center).WithMany()
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WO_Centers");

            entity.HasOne(d => d.Service).WithMany()
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WO_Services");
        });

        modelBuilder.Entity<MaintenanceChecklist>(entity =>
        {
            entity.HasKey(e => e.ChecklistId).HasName("PK_MaintenanceChecklist");
            entity.ToTable("MaintenanceChecklists", "dbo");
            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.VehicleModelPartId).HasColumnName("VehicleModelPartID");
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.MaintenanceChecklists)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MChecklist_WorkOrders");

            entity.HasOne(d => d.VehicleModelPart).WithMany()
                .HasForeignKey(d => d.VehicleModelPartId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_MChecklist_VehicleModelParts");
        });

        modelBuilder.Entity<WorkOrderPart>(entity =>
        {
            entity.HasKey(e => new { e.WorkOrderId, e.PartId });

            entity.ToTable("WorkOrderParts", "dbo");

            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.VehicleModelPartId).HasColumnName("VehicleModelPartID");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Part).WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WOP_Parts");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WOP_WorkOrders");

            entity.HasOne(d => d.VehicleModelPart).WithMany()
                .HasForeignKey(d => d.VehicleModelPartId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WOP_VehicleModelParts");

            entity.HasIndex(e => e.VehicleModelPartId).HasDatabaseName("IX_WOP_VehicleModelPartID");
        });
        // Removed sequence for InvoiceNumber

        // E-commerce tables configuration - ShoppingCarts removed

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFE9A32E1C");
            entity.ToTable("Orders", "dbo");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Status).HasDefaultValue("PENDING");
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customers");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderItem__E4F5E4C8E9A32E1C");
            entity.ToTable("OrderItems", "dbo");
            entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Part).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Parts");
        });

        // Removed: OrderStatusHistory mapping

        

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK_Feedbacks");
            entity.ToTable("Feedbacks", "dbo");
            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            // IsVerified removed
            // IsVerified removed
            entity.Property(e => e.IsAnonymous).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedbacks_Customers");

            entity.HasOne(d => d.Order).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedbacks_Orders");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedbacks_WorkOrders");
            
            entity.HasOne(d => d.Part).WithMany()
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedbacks_Parts");

            entity.HasOne(d => d.Technician).WithMany()
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedbacks_Technicians");
            
        });


        // VehicleModel configuration
        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.HasKey(e => e.ModelId).HasName("PK_VehicleModel");

            entity.ToTable("VehicleModel", "dbo");

            entity.HasIndex(e => new { e.ModelName, e.Brand }, "UK_VehicleModel_Name_Brand")
                .IsUnique();

            entity.Property(e => e.ModelId).HasColumnName("ModelID");
            entity.Property(e => e.ModelName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Brand)
                .IsRequired()
                .HasMaxLength(50);
            // Removed specs mapping for VehicleModel
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            // UpdatedAt removed - not in database
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        // VehicleModelPart configuration
        modelBuilder.Entity<VehicleModelPart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VehicleModelParts");

            entity.ToTable("VehicleModelParts", "dbo");

            entity.HasIndex(e => new { e.ModelId, e.PartId }, "UK_VehicleModelParts")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModelId).HasColumnName("ModelID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.CompatibilityNotes).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsCompatible)
                .HasDefaultValue(true);

            entity.HasOne(d => d.VehicleModel)
                .WithMany(p => p.VehicleModelParts)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_VehicleModelParts_VehicleModel");

            entity.HasOne(d => d.Part)
                .WithMany(p => p.VehicleModelParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_VehicleModelParts_Parts");
        });

        modelBuilder.Entity<ServiceRequiredSkill>(entity =>
        {
            entity.HasKey(e => new { e.ServiceId, e.SkillId });
            entity.ToTable("ServiceRequiredSkills", "dbo");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.SkillId).HasColumnName("SkillID");

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SRS_Services");

            entity.HasOne(e => e.Skill)
                .WithMany()
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SRS_Skills");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
