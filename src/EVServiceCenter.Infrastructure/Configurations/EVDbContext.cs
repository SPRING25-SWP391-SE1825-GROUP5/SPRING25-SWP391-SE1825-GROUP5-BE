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

    public  DbSet<BookingService> BookingServices { get; set; }

    public  DbSet<BookingTimeSlot> BookingTimeSlots { get; set; }

    public  DbSet<Channel> Channels { get; set; }

    public  DbSet<Customer> Customers { get; set; }

    public  DbSet<Inventory> Inventories { get; set; }

    public  DbSet<InventoryBalance> InventoryBalances { get; set; }

    public  DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public  DbSet<InventoryTransfer> InventoryTransfers { get; set; }

    public  DbSet<InventoryTransferItem> InventoryTransferItems { get; set; }

    public  DbSet<Invoice> Invoices { get; set; }

    public  DbSet<InvoiceItem> InvoiceItems { get; set; }

    public  DbSet<InvoicePayment> InvoicePayments { get; set; }

    public  DbSet<LeaveRequest> LeaveRequests { get; set; }

    public  DbSet<MaintenanceChecklist> MaintenanceChecklists { get; set; }

    public  DbSet<MaintenanceChecklistItem> MaintenanceChecklistItems { get; set; }

    public  DbSet<MaintenanceChecklistResult> MaintenanceChecklistResults { get; set; }

    public  DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }

    public  DbSet<Notification> Notifications { get; set; }

    public  DbSet<Otpcode> Otpcodes { get; set; }

    public  DbSet<Part> Parts { get; set; }

    public  DbSet<Payment> Payments { get; set; }

    public  DbSet<Promotion> Promotions { get; set; }

    public  DbSet<SalesOrder> SalesOrders { get; set; }

    public  DbSet<SalesOrderItem> SalesOrderItems { get; set; }

    public  DbSet<Service> Services { get; set; }

    public  DbSet<ServiceCategory> ServiceCategories { get; set; }

    public  DbSet<ServiceCenter> ServiceCenters { get; set; }

    public  DbSet<ServicePackage> ServicePackages { get; set; }

    public  DbSet<ServicePackageItem> ServicePackageItems { get; set; }

    public  DbSet<Staff> Staff { get; set; }

    public  DbSet<SystemSetting> SystemSettings { get; set; }

    public  DbSet<Technician> Technicians { get; set; }

    public  DbSet<CenterSchedule> CenterSchedules { get; set; }

    public  DbSet<TechnicianTimeSlot> TechnicianTimeSlots { get; set; }

    public  DbSet<TimeSlot> TimeSlots { get; set; }

    public  DbSet<User> Users { get; set; }

    public  DbSet<UserPromotion> UserPromotions { get; set; }

    public  DbSet<Vehicle> Vehicles { get; set; }

    public  DbSet<VehicleModel> VehicleModels { get; set; }

    public  DbSet<VwAvailableSeat> VwAvailableSeats { get; set; }

    public  DbSet<Warehouse> Warehouses { get; set; }

    public  DbSet<WorkOrder> WorkOrders { get; set; }

    public  DbSet<WorkOrderChargeProposal> WorkOrderChargeProposals { get; set; }

    public  DbSet<WorkOrderChargeProposalItem> WorkOrderChargeProposalItems { get; set; }

    public  DbSet<WorkOrderPart> WorkOrderParts { get; set; }

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
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.SpecialRequests).HasMaxLength(500);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.TotalEstimatedCost).HasColumnType("decimal(12, 2)");
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

            entity.HasOne(d => d.Slot).WithMany()
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Slot");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Vehicles");
        });

        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(e => new { e.BookingId, e.ServiceId });

            entity.ToTable("BookingServices", "dbo");

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_Bookings");

            entity.HasOne(d => d.Service).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_Services");
        });

        modelBuilder.Entity<BookingTimeSlot>(entity =>
        {
            entity.HasKey(e => new { e.BookingId, e.SlotId });

            entity.ToTable("BookingTimeSlots", "dbo", tb =>
                {
                    tb.HasTrigger("TRG_BTS_Prevent_Tech_DoubleBook");
                    tb.HasTrigger("TRG_BTS_Release_Tech_Slot");
                });

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.SlotId).HasColumnName("SlotID");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingTimeSlots)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BTS_Bookings");

            entity.HasOne(d => d.Slot).WithMany(p => p.BookingTimeSlots)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BTS_TimeSlots");

            entity.HasOne(d => d.Technician).WithMany(p => p.BookingTimeSlots)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BTS_Technicians");
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("Channels", "dbo");

            entity.HasIndex(e => e.Code, "UQ_Channels_Code").IsUnique();

            entity.Property(e => e.ChannelId).HasColumnName("ChannelID");
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
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

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.HasKey(e => new { e.PartId, e.WarehouseId });

            entity.ToTable("InventoryBalances", "dbo");

            entity.HasIndex(e => e.PartId, "IX_InventoryBalances_Part");

            entity.HasIndex(e => e.WarehouseId, "IX_InventoryBalances_Warehouse");

            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Part).WithMany(p => p.InventoryBalances)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryBalances_Parts");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.InventoryBalances)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryBalances_Warehouses");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);

            entity.ToTable("InventoryTransactions", "dbo", tb => tb.HasTrigger("trg_NoNegativeStock"));

            entity.HasIndex(e => e.CreatedAt, "IX_InvTrans_CreatedAt");

            entity.HasIndex(e => new { e.PartId, e.WarehouseId }, "IX_InvTrans_Part_Warehouse");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.RefId).HasColumnName("RefID");
            entity.Property(e => e.RefType)
                .IsRequired()
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Part).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTrans_Parts");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTrans_Warehouses");
        });

        modelBuilder.Entity<InventoryTransfer>(entity =>
        {
            entity.HasKey(e => e.TransferId);

            entity.ToTable("InventoryTransfers", "dbo");

            entity.HasIndex(e => e.FromWarehouseId, "IX_InvTransfers_FromWh");

            entity.HasIndex(e => e.Status, "IX_InvTransfers_Status");

            entity.HasIndex(e => e.ToWarehouseId, "IX_InvTransfers_ToWh");

            entity.Property(e => e.TransferId).HasColumnName("TransferID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FromWarehouseId).HasColumnName("FromWarehouseID");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PostedAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.ToWarehouseId).HasColumnName("ToWarehouseID");

            entity.HasOne(d => d.FromWarehouse).WithMany(p => p.InventoryTransferFromWarehouses)
                .HasForeignKey(d => d.FromWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTransfers_FromWh");

            entity.HasOne(d => d.ToWarehouse).WithMany(p => p.InventoryTransferToWarehouses)
                .HasForeignKey(d => d.ToWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTransfers_ToWh");
        });

        modelBuilder.Entity<InventoryTransferItem>(entity =>
        {
            entity.HasKey(e => new { e.TransferId, e.PartId }).HasName("PK_InvTransferItems");

            entity.ToTable("InventoryTransferItems", "dbo");

            entity.Property(e => e.TransferId).HasColumnName("TransferID");
            entity.Property(e => e.PartId).HasColumnName("PartID");

            entity.HasOne(d => d.Part).WithMany(p => p.InventoryTransferItems)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTransferItems_Part");

            entity.HasOne(d => d.Transfer).WithMany(p => p.InventoryTransferItems)
                .HasForeignKey(d => d.TransferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvTransferItems_Transfer");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAD52C746B3F");

            entity.ToTable("Invoices", "dbo");

            entity.HasIndex(e => e.InvoiceType, "IX_Invoices_InvoiceType");

            entity.HasIndex(e => e.NormalizedBillingPhone, "IX_Invoices_NormPhone");

            entity.HasIndex(e => e.ParentInvoiceId, "IX_Invoices_Parent");

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
            entity.Property(e => e.ParentInvoiceId).HasColumnName("ParentInvoiceID");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Invoices_Customers");

            entity.HasOne(d => d.WorkOrder).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.WorkOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_WorkOrders");
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

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__InvoiceIt__Invoi__30441BD6");

            entity.HasOne(d => d.Part).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.PartId)
                .HasConstraintName("FK__InvoiceIt__PartI__3138400F");
        });

        modelBuilder.Entity<InvoicePayment>(entity =>
        {
            entity.HasKey(e => new { e.InvoiceId, e.PaymentId });

            entity.ToTable("InvoicePayments", "dbo");

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.AppliedAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoicePayments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__InvoicePa__Invoi__2B7F66B9");

            entity.HasOne(d => d.Payment).WithMany(p => p.InvoicePayments)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__InvoicePa__Payme__2C738AF2");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__LeaveReq__33A8519A3EC1FA1C");

            entity.ToTable("LeaveRequests", "dbo");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.Comments).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.LeaveType)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_LR_Approver");

            entity.HasOne(d => d.Technician).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.TechnicianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LR_Tech");
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

        modelBuilder.Entity<MaintenanceChecklistItem>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__Maintena__727E83EB43DEA097");

            entity.ToTable("MaintenanceChecklistItems", "dbo");

            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ItemName)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<MaintenanceChecklistResult>(entity =>
        {
            entity.HasKey(e => new { e.ChecklistId, e.ItemId });

            entity.ToTable("MaintenanceChecklistResults", "dbo");

            entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.Comment).HasMaxLength(250);
            entity.Property(e => e.Result).HasMaxLength(50);

            entity.HasOne(d => d.Checklist).WithMany(p => p.MaintenanceChecklistResults)
                .HasForeignKey(d => d.ChecklistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Check__5887175A");

            entity.HasOne(d => d.Item).WithMany(p => p.MaintenanceChecklistResults)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__ItemI__597B3B93");
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

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("SalesOrders", "dbo");

            entity.HasIndex(e => e.ChannelId, "IX_SalesOrders_Channel");

            entity.HasIndex(e => e.Status, "IX_SalesOrders_Status");

            entity.HasIndex(e => e.WarehouseId, "IX_SalesOrders_Warehouse");

            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.ChannelId).HasColumnName("ChannelID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

            entity.HasOne(d => d.Center).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrders_ServiceCtrs");

            entity.HasOne(d => d.Channel).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.ChannelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrders_Channels");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_SalesOrders_Customers");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrders_Warehouses");
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.HasKey(e => new { e.SalesOrderId, e.PartId });

            entity.ToTable("SalesOrderItems", "dbo");

            entity.HasIndex(e => e.PartId, "IX_SalesOrderItems_Part");

            entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");
            entity.Property(e => e.PartId).HasColumnName("PartID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Part).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.PartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrderItems_Parts");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrderItems_Order");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB0EAE5210AEC");

            entity.ToTable("Services", "dbo");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RequiredSkills).HasMaxLength(255);
            entity.Property(e => e.ServiceName)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.Services)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_Categories");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ServiceC__19093A2BA295C1B4");

            entity.ToTable("ServiceCategories", "dbo");

            entity.HasIndex(e => e.ParentCategoryId, "IX_ServiceCategories_Parent");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ParentCategoryId).HasColumnName("ParentCategoryID");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK_ServiceCategories_Parent");
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

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__ServiceP__322035EC20239E6E");

            entity.ToTable("ServicePackages", "dbo");

            entity.HasIndex(e => e.PackageCode, "UQ__ServiceP__94185429C3E364E2").IsUnique();

            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PackageCode).HasMaxLength(50);
            entity.Property(e => e.PackageName)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<ServicePackageItem>(entity =>
        {
            entity.HasKey(e => new { e.PackageId, e.ServiceId });

            entity.ToTable("ServicePackageItems", "dbo");

            entity.HasIndex(e => new { e.PackageId, e.SortOrder }, "IX_SPI_Package");

            entity.HasIndex(e => e.ServiceId, "IX_SPI_Service");

            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Package).WithMany(p => p.ServicePackageItems)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SPI_Packages");

            entity.HasOne(d => d.Service).WithMany(p => p.ServicePackageItems)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SPI_Services");
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
            entity.Property(e => e.SlotTime).HasPrecision(0);
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
            entity.Property(e => e.ModelId).HasColumnName("ModelID");
            entity.Property(e => e.Vin)
                .IsRequired()
                .HasMaxLength(17)
                .HasColumnName("VIN");

            entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicles_Customers");

            entity.HasOne(d => d.Model).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicles_Models");
        });

        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.HasKey(e => e.ModelId).HasName("PK__VehicleM__E8D7A1CCEDFB4F23");

            entity.ToTable("VehicleModels", "dbo");

            entity.Property(e => e.ModelId).HasColumnName("ModelID");
            entity.Property(e => e.BatteryCapacity).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Brand)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ModelName)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<VwAvailableSeat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_Available_Seats");

            entity.Property(e => e.CinemaRoomId).HasColumnName("Cinema_Room_ID");
            entity.Property(e => e.ColumnNumber).HasColumnName("Column_Number");
            entity.Property(e => e.MovieId).HasColumnName("Movie_ID");
            entity.Property(e => e.RowLabel)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("Row_Label");
            entity.Property(e => e.SeatId).HasColumnName("Seat_ID");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Seat_Number");
            entity.Property(e => e.SeatType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Seat_Type");
            entity.Property(e => e.ShowDate).HasColumnName("Show_Date");
            entity.Property(e => e.ShowtimeId).HasColumnName("Showtime_ID");
            entity.Property(e => e.StartTime).HasColumnName("Start_Time");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(9)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouses", "dbo");

            entity.HasIndex(e => e.CenterId, "IX_Warehouses_Center");

            entity.HasIndex(e => new { e.CenterId, e.Code }, "UQ_Warehouses_Center_Code").IsUnique();

            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.CenterId).HasColumnName("CenterID");
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(d => d.Center).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouses_ServiceCenters");
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
            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.StartTime).HasPrecision(0);
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
