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

    public  DbSet<CenterSchedule> CenterSchedules { get; set; }

    public  DbSet<Customer> Customers { get; set; }

    public  DbSet<Inventory> Inventories { get; set; }






    public  DbSet<Invoice> Invoices { get; set; }

    public  DbSet<InvoiceItem> InvoiceItems { get; set; }
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

    public  DbSet<ServiceCreditUsage> ServiceCreditUsages { get; set; }

    public  DbSet<Staff> Staff { get; set; }

    public  DbSet<SystemSetting> SystemSettings { get; set; }

    public  DbSet<Technician> Technicians { get; set; }

    public  DbSet<TechnicianTimeSlot> TechnicianTimeSlots { get; set; }
    public  DbSet<Skill> Skills { get; set; }
    public  DbSet<TechnicianSkill> TechnicianSkills { get; set; }
    public  DbSet<SeveritySkillRequirement> SeveritySkillRequirements { get; set; }

    public  DbSet<TimeSlot> TimeSlots { get; set; }

    public  DbSet<User> Users { get; set; }

    public  DbSet<UserPromotion> UserPromotions { get; set; }

    public  DbSet<Vehicle> Vehicles { get; set; }

    public  DbSet<WorkOrder> WorkOrders { get; set; }

    public  DbSet<WorkOrderChargeProposal> WorkOrderChargeProposals { get; set; }

    public  DbSet<WorkOrderChargeProposalItem> WorkOrderChargeProposalItems { get; set; }

    public  DbSet<WorkOrderPart> WorkOrderParts { get; set; }
    public  DbSet<MaintenancePolicy> MaintenancePolicies { get; set; }

    // E-commerce tables
    public  DbSet<ShoppingCart> ShoppingCarts { get; set; }

    public  DbSet<Order> Orders { get; set; }

    public  DbSet<OrderItem> OrderItems { get; set; }

    public  DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public  DbSet<Wishlist> Wishlists { get; set; }

    public  DbSet<ProductReview> ProductReviews { get; set; }

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

            entity.HasIndex(e => e.BookingCode, "UQ__Bookings__C6E56BD5C752B9AD").IsUnique();

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(20);
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
            entity.Property(e => e.TotalEstimatedCost).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Severity).HasColumnType("tinyint").HasDefaultValue((byte)2);
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

        modelBuilder.Entity<CenterSchedule>(entity =>
        {
            entity.HasKey(e => e.CenterScheduleId).HasName("PK__CenterSc__D7B6B8F4E9A32E1C");

            entity.ToTable("CenterSchedule", "dbo");

            entity.Property(e => e.CenterScheduleId).HasColumnName("CenterScheduleID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ScheduleDate).HasColumnType("date");
            entity.Property(e => e.StartTime).HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));
            entity.Property(e => e.EndTime).HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));

            entity.HasOne(d => d.Center).WithMany(p => p.CenterSchedules)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CenterSch__Cente__3F466844");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B8CC15439B");

            entity.ToTable("Customers", "dbo");

            entity.HasIndex(e => e.CustomerCode, "UQ__Customer__066785211CB53DFC").IsUnique();

            entity.HasIndex(e => e.NormalizedPhone, "UX_Customers_GuestPhone")
                .IsUnique()
                .HasFilter("([IsGuest]=(1) AND [NormalizedPhone] IS NOT NULL AND [NormalizedPhone]<>N'')");

            entity.HasIndex(e => e.UserId, "UX_Customers_UserID_NotNull")
                .IsUnique()
                .HasFilter("([UserID] IS NOT NULL)");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerCode)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.IsGuest).HasDefaultValue(true);
            entity.Property(e => e.NormalizedPhone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .HasConstraintName("FK_Customers_Users");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__F5FDE6D35C44F64D");

            entity.ToTable("Inventory", "dbo");

            entity.HasIndex(e => new { e.CenterId, e.PartId }, "UQ_Inv_CenterPart").IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.LastUpdated)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PartId).HasColumnName("PartID");

            entity.HasOne(d => d.Center).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inv_Centers");

            entity.HasOne(d => d.Part).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inv_Parts");
        });





        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAD52C746B3F");

            entity.ToTable("Invoices", "dbo");

            entity.HasIndex(e => e.InvoiceType, "IX_Invoices_InvoiceType");

            entity.HasIndex(e => e.NormalizedBillingPhone, "IX_Invoices_NormPhone");

            // entity.HasIndex(e => e.ParentInvoiceId, "IX_Invoices_Parent"); // Column không tồn tại trong database

            entity.HasIndex(e => e.Status, "IX_Invoices_Status");

            entity.HasIndex(e => e.InvoiceNumber, "UQ__Invoices__D776E9818A87403A").IsUnique();

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.BillingAddress).HasMaxLength(255);
            entity.Property(e => e.BillingName)
                .IsRequired()
                .HasMaxLength(200)
                .HasDefaultValue("Guest");
            entity.Property(e => e.BillingPhone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.InvoiceType)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("STANDARD");
            entity.Property(e => e.NormalizedBillingPhone)
                .HasMaxLength(20)
                .HasComputedColumnSql("(left(replace(replace(replace(replace(isnull([BillingPhone],N''),N' ',N''),N'-',N''),N'(',N''),N')',N''),(20)))", true);
            // entity.Property(e => e.ParentInvoiceId).HasColumnName("ParentInvoiceID"); // Column không tồn tại trong database
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
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
                .HasConstraintName("FK_Invoices_Orders");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.InvoiceItemId).HasName("PK__InvoiceI__478FE0FC9644E7D3");

            entity.ToTable("InvoiceItems", "dbo");

            entity.HasIndex(e => e.InvoiceId, "IX_InvoiceItems_InvoiceID");

            entity.Property(e => e.InvoiceItemId).HasColumnName("InvoiceItemID");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__InvoiceIt__Invoi__30441BD6");

            entity.HasOne(d => d.Part).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.PartId)
                .HasConstraintName("FK__InvoiceIt__PartI__3138400F");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK_InvoiceItems_OrderItems");
        });



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
            entity.Property(e => e.ServiceType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.MaintenanceReminders)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MR_Vehicles");
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
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(20);
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
            entity.Property(e => e.Unit)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A58C7B37C95");

            entity.ToTable("Payments", "dbo", tb => tb.HasTrigger("tr_Payments_DefaultBuyerFromInvoice"));

            entity.HasIndex(e => e.CreatedAt, "IX_Payments_Created");

            entity.HasIndex(e => e.Status, "IX_Payments_Status");

            entity.HasIndex(e => e.PayOsorderCode, "UQ__Payments__0426C86F4FABC463").IsUnique();

            entity.HasIndex(e => e.PaymentCode, "UQ__Payments__106D3BA8E41F72D2").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.BuyerAddress).HasMaxLength(255);
            entity.Property(e => e.BuyerName).HasMaxLength(100);
            entity.Property(e => e.BuyerPhone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.PayOsorderCode).HasColumnName("PayOSOrderCode");
            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("PAYOS");
            entity.Property(e => e.PaidByUserId).HasColumnName("PaidByUserId");
            entity.Property(e => e.AttemptNo)
                .HasDefaultValue(1);
            entity.Property(e => e.AttemptStatus)
                .HasMaxLength(20);
            entity.Property(e => e.AttemptAt)
                .HasPrecision(0);
            entity.Property(e => e.AttemptMessage)
                .HasMaxLength(255);
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
            entity.Property(e => e.ApplyFor)
                .IsRequired()
                .HasMaxLength(20);
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
            entity.Property(e => e.PromotionType)
                .IsRequired()
                .HasMaxLength(20);
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
            entity.Property(e => e.RequiredSkills).HasMaxLength(255);
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
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PricePaid).HasColumnType("decimal(10, 2)");
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

            entity.HasOne(d => d.Invoice).WithMany()
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCr__Invoi__412EB0B6");
        });

        modelBuilder.Entity<ServiceCreditUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PK__ServiceC__A4D4B8F4E9A32E1C");

            entity.ToTable("ServiceCreditUsages", "dbo");

            entity.Property(e => e.UsageId).HasColumnName("UsageID");
            entity.Property(e => e.CreditId).HasColumnName("CreditID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.UsedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Credit).WithMany(p => p.ServiceCreditUsages)
                .HasForeignKey(d => d.CreditId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCr__Credi__3F466844");

            entity.HasOne(d => d.Booking).WithMany(p => p.ServiceCreditUsages)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCr__Booki__403A8C7D");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.ServiceCreditUsages)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCr__WorkO__412EB0B6");
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
            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });



        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AAF70CFA06C8");

            entity.ToTable("Staff", "dbo");

            entity.HasIndex(e => e.StaffCode, "UQ__Staff__D83AD812AE8FD68C").IsUnique();

            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Position)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.StaffCode)
                .IsRequired()
                .HasMaxLength(20);
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

            entity.HasIndex(e => e.TechnicianCode, "UQ__Technici__ED64BD1AD2C78212").IsUnique();

            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Specialization).HasMaxLength(100);
            entity.Property(e => e.TechnicianCode)
                .IsRequired()
                .HasMaxLength(20);
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
            entity.Property(e => e.Level).HasDefaultValue((byte)3);
            entity.Property(e => e.Years).HasDefaultValue((byte)0);

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

        modelBuilder.Entity<SeveritySkillRequirement>(entity =>
        {
            entity.HasKey(e => new { e.Severity, e.SkillId });
            entity.ToTable("SeveritySkillRequirements", "dbo");
            entity.Property(e => e.Severity).HasColumnType("tinyint");
            entity.Property(e => e.SkillId).HasColumnName("SkillID");
            entity.Property(e => e.MinLevel).HasColumnType("tinyint");
            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(e => e.Skill)
                .WithMany()
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SeveritySkillReq_Skills");
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
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            entity.Property(e => e.LockoutUntil);
        });

        modelBuilder.Entity<UserPromotion>(entity =>
        {
            entity.ToTable("UserPromotions", "dbo");

            entity.HasIndex(e => new { e.CustomerId, e.UsedAt }, "IX_UserPromotions_Customer").IsDescending(false, true);

            entity.HasIndex(e => new { e.PromotionId, e.UsedAt }, "IX_UserPromotions_Promotion").IsDescending(false, true);

            entity.HasIndex(e => new { e.PromotionId, e.InvoiceId }, "UQ_UserPromotions_Promo_Invoice")
                .IsUnique()
                .HasFilter("([InvoiceID] IS NOT NULL)");

            entity.Property(e => e.UserPromotionId).HasColumnName("UserPromotionID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
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

            entity.HasOne(d => d.Invoice).WithMany(p => p.UserPromotions)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_UserPromotions_Invoices");

            entity.HasOne(d => d.Promotion).WithMany(p => p.UserPromotions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPromotions_Promotions");
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

            entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicles_Customers");
        });




        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.WorkOrderId).HasName("PK__WorkOrde__AE75517563E7EB82");

            entity.ToTable("WorkOrders", "dbo");

            entity.HasIndex(e => e.WorkOrderNumber, "UQ__WorkOrde__1FA44F96DE057B33").IsUnique();

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
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.WorkOrderNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasOne(d => d.Booking).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WO_Bookings");

            entity.HasOne(d => d.Technician).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WO_Technicians");
        });

        modelBuilder.Entity<WorkOrderChargeProposal>(entity =>
        {
            entity.HasKey(e => e.ProposalId).HasName("PK__WorkOrde__6F39E100BDB375C0");

            entity.ToTable("WorkOrderChargeProposals", "dbo");

            entity.Property(e => e.ProposalId).HasColumnName("ProposalID");
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.ApprovedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.WorkOrderChargeProposals)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkOrder__WorkO__200DB40D");
        });

        modelBuilder.Entity<WorkOrderChargeProposalItem>(entity =>
        {
            entity.HasKey(e => e.ProposalItemId).HasName("PK_WOCPItems");

            entity.ToTable("WorkOrderChargeProposalItems", "dbo");

            entity.HasIndex(e => new { e.ProposalId, e.PartId }, "UX_WOCPItems_Proposal_Part_NotNull")
                .IsUnique()
                .HasFilter("([PartID] IS NOT NULL)");

            entity.Property(e => e.ProposalItemId).HasColumnName("ProposalItemID");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.ProposalId).HasColumnName("ProposalID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Part).WithMany(p => p.WorkOrderChargeProposalItems)
                .HasForeignKey(d => d.PartId)
                .HasConstraintName("FK__WorkOrder__PartI__25C68D63");

            entity.HasOne(d => d.Proposal).WithMany(p => p.WorkOrderChargeProposalItems)
                .HasForeignKey(d => d.ProposalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WorkOrder__Propo__24D2692A");
        });

        modelBuilder.Entity<WorkOrderPart>(entity =>
        {
            entity.HasKey(e => new { e.WorkOrderId, e.PartId });

            entity.ToTable("WorkOrderParts", "dbo");

            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Part).WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WOP_Parts");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.WorkOrderParts)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WOP_WorkOrders");
        });
        modelBuilder.HasSequence("Seq_InvoiceNumber", "dbo").StartsAt(100000L);

        // E-commerce tables configuration
        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Shopping__C52A0BB3E9A32E1C");
            entity.ToTable("ShoppingCarts", "dbo");
            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.ShoppingCarts)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShoppingCarts_Customers");

            entity.HasOne(d => d.Part).WithMany(p => p.ShoppingCarts)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShoppingCarts_Parts");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFE9A32E1C");
            entity.ToTable("Orders", "dbo");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
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
            entity.Property(e => e.LineTotal).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Part).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Parts");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderStat__2D7B4C4FE9A32E1C");
            entity.ToTable("OrderStatusHistory", "dbo");
            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.SystemGenerated).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderStatusHistory_Orders");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderStatusHistory_Users");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__Wishlist__8A2B2B8EE9A32E1C");
            entity.ToTable("Wishlists", "dbo");
            entity.Property(e => e.WishlistId).HasColumnName("WishlistID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wishlists_Customers");

            entity.HasOne(d => d.Part).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wishlists_Parts");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__ProductRe__74BC79CEE9A32E1C");
            entity.ToTable("ProductReviews", "dbo");
            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Part).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductReviews_Parts");

            entity.HasOne(d => d.Customer).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductReviews_Customers");

            entity.HasOne(d => d.Order).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductReviews_Orders");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
