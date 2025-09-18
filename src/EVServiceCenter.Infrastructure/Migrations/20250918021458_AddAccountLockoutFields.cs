using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVServiceCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence(
                name: "Seq_InvoiceNumber",
                schema: "dbo",
                startValue: 100000L);

            migrationBuilder.CreateTable(
                name: "Channels",
                schema: "dbo",
                columns: table => new
                {
                    ChannelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.ChannelID);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistItems",
                schema: "dbo",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__727E83EB43DEA097", x => x.ItemID);
                });

            migrationBuilder.CreateTable(
                name: "Parts",
                schema: "dbo",
                columns: table => new
                {
                    PartID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Parts__7C3F0D3048808AFE", x => x.PartID);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "dbo",
                columns: table => new
                {
                    PromotionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountValue = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaxDiscount = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    UserLimit = table.Column<int>(type: "int", nullable: true),
                    PromotionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApplyFor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.PromotionID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                schema: "dbo",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ParentCategoryID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ServiceC__19093A2BA295C1B4", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_ServiceCategories_Parent",
                        column: x => x.ParentCategoryID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCategories",
                        principalColumn: "CategoryID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceCenters",
                schema: "dbo",
                columns: table => new
                {
                    CenterID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ServiceC__398FC7D760929C24", x => x.CenterID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                schema: "dbo",
                columns: table => new
                {
                    PackageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PackageName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ServiceP__322035EC20239E6E", x => x.PackageID);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "dbo",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SystemSe__01E719AC5928C2F7", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                schema: "dbo",
                columns: table => new
                {
                    SlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlotTime = table.Column<TimeOnly>(type: "time(0)", precision: 0, nullable: false),
                    SlotLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TimeSlot__0A124A4F5ADA88F1", x => x.SlotID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    RefreshToken = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCAC0F533A1C", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                schema: "dbo",
                columns: table => new
                {
                    ModelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    BatteryCapacity = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Range = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VehicleM__E8D7A1CCEDFB4F23", x => x.ModelID);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "dbo",
                columns: table => new
                {
                    ServiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedDuration = table.Column<int>(type: "int", nullable: false),
                    RequiredSlots = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Services__C51BB0EAE5210AEC", x => x.ServiceID);
                    table.ForeignKey(
                        name: "FK_Services_Categories",
                        column: x => x.CategoryID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCategories",
                        principalColumn: "CategoryID");
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                schema: "dbo",
                columns: table => new
                {
                    InventoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__F5FDE6D35C44F64D", x => x.InventoryID);
                    table.ForeignKey(
                        name: "FK_Inv_Centers",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Inv_Parts",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "dbo",
                columns: table => new
                {
                    WarehouseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseID);
                    table.ForeignKey(
                        name: "FK_Warehouses_ServiceCenters",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "dbo",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    CustomerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NormalizedPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsGuest = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Customer__A4AE64B8CC15439B", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_Customers_Users",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "dbo",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__20CF2E32AE77D9ED", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Noti_Users",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "OTPCodes",
                schema: "dbo",
                columns: table => new
                {
                    OTPID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    OTPCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OTPType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OTPCodes__5C2EC56253E54938", x => x.OTPID);
                    table.ForeignKey(
                        name: "FK_OTPCodes_Users",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                schema: "dbo",
                columns: table => new
                {
                    StaffID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    StaffCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Staff__96D4AAF70CFA06C8", x => x.StaffID);
                    table.ForeignKey(
                        name: "FK_Staff_Centers",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Staff_Users",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                schema: "dbo",
                columns: table => new
                {
                    TechnicianID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    TechnicianCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExperienceYears = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Technici__301F82C180EA1203", x => x.TechnicianID);
                    table.ForeignKey(
                        name: "FK_Tech_Centers",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Tech_Users",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ServicePackageItems",
                schema: "dbo",
                columns: table => new
                {
                    PackageID = table.Column<int>(type: "int", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", nullable: false, defaultValue: 1m),
                    SortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackageItems", x => new { x.PackageID, x.ServiceID });
                    table.ForeignKey(
                        name: "FK_SPI_Packages",
                        column: x => x.PackageID,
                        principalSchema: "dbo",
                        principalTable: "ServicePackages",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_SPI_Services",
                        column: x => x.ServiceID,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                schema: "dbo",
                columns: table => new
                {
                    PartID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => new { x.PartID, x.WarehouseID });
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Parts",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Warehouses",
                        column: x => x.WarehouseID,
                        principalSchema: "dbo",
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                schema: "dbo",
                columns: table => new
                {
                    TransactionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    QtyChange = table.Column<int>(type: "int", nullable: false),
                    RefType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    RefID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_InvTrans_Parts",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK_InvTrans_Warehouses",
                        column: x => x.WarehouseID,
                        principalSchema: "dbo",
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransfers",
                schema: "dbo",
                columns: table => new
                {
                    TransferID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromWarehouseID = table.Column<int>(type: "int", nullable: false),
                    ToWarehouseID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    PostedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransfers", x => x.TransferID);
                    table.ForeignKey(
                        name: "FK_InvTransfers_FromWh",
                        column: x => x.FromWarehouseID,
                        principalSchema: "dbo",
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                    table.ForeignKey(
                        name: "FK_InvTransfers_ToWh",
                        column: x => x.ToWarehouseID,
                        principalSchema: "dbo",
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "dbo",
                columns: table => new
                {
                    SalesOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: true),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    ChannelID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.SalesOrderID);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Channels",
                        column: x => x.ChannelID,
                        principalSchema: "dbo",
                        principalTable: "Channels",
                        principalColumn: "ChannelID");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_SalesOrders_ServiceCtrs",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Warehouses",
                        column: x => x.WarehouseID,
                        principalSchema: "dbo",
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                schema: "dbo",
                columns: table => new
                {
                    VehicleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    ModelID = table.Column<int>(type: "int", nullable: false),
                    VIN = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CurrentMileage = table.Column<int>(type: "int", nullable: false),
                    LastServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextServiceDue = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vehicles__476B54B2FA36B2E5", x => x.VehicleID);
                    table.ForeignKey(
                        name: "FK_Vehicles_Customers",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Vehicles_Models",
                        column: x => x.ModelID,
                        principalSchema: "dbo",
                        principalTable: "VehicleModels",
                        principalColumn: "ModelID");
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "dbo",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LeaveReq__33A8519A3EC1FA1C", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_LR_Approver",
                        column: x => x.ApprovedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_LR_Tech",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID");
                });

            migrationBuilder.CreateTable(
                name: "TechnicianTimeSlots",
                schema: "dbo",
                columns: table => new
                {
                    TechnicianSlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SlotID = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsBooked = table.Column<bool>(type: "bit", nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Technici__8892BB75E5FCFE53", x => x.TechnicianSlotID);
                    table.ForeignKey(
                        name: "FK_TTS_Slot",
                        column: x => x.SlotID,
                        principalSchema: "dbo",
                        principalTable: "TimeSlots",
                        principalColumn: "SlotID");
                    table.ForeignKey(
                        name: "FK_TTS_Tech",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransferItems",
                schema: "dbo",
                columns: table => new
                {
                    TransferID = table.Column<long>(type: "bigint", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvTransferItems", x => new { x.TransferID, x.PartID });
                    table.ForeignKey(
                        name: "FK_InvTransferItems_Part",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK_InvTransferItems_Transfer",
                        column: x => x.TransferID,
                        principalSchema: "dbo",
                        principalTable: "InventoryTransfers",
                        principalColumn: "TransferID");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                schema: "dbo",
                columns: table => new
                {
                    SalesOrderID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => new { x.SalesOrderID, x.PartID });
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Order",
                        column: x => x.SalesOrderID,
                        principalSchema: "dbo",
                        principalTable: "SalesOrders",
                        principalColumn: "SalesOrderID");
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Parts",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "dbo",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    VehicleID = table.Column<int>(type: "int", nullable: false),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    BookingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartSlotID = table.Column<int>(type: "int", nullable: false),
                    EndSlotID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    SpecialRequests = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    TotalSlots = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(([EndSlotID]-[StartSlotID])+(1))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Bookings__73951ACD58E24216", x => x.BookingID);
                    table.ForeignKey(
                        name: "FK_Book_Centers",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Book_Customers",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Book_EndSlot",
                        column: x => x.EndSlotID,
                        principalSchema: "dbo",
                        principalTable: "TimeSlots",
                        principalColumn: "SlotID");
                    table.ForeignKey(
                        name: "FK_Book_StartSlot",
                        column: x => x.StartSlotID,
                        principalSchema: "dbo",
                        principalTable: "TimeSlots",
                        principalColumn: "SlotID");
                    table.ForeignKey(
                        name: "FK_Book_Vehicles",
                        column: x => x.VehicleID,
                        principalSchema: "dbo",
                        principalTable: "Vehicles",
                        principalColumn: "VehicleID");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceReminders",
                schema: "dbo",
                columns: table => new
                {
                    ReminderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleID = table.Column<int>(type: "int", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DueMileage = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__01A830A779AB1D09", x => x.ReminderID);
                    table.ForeignKey(
                        name: "FK_MR_Vehicles",
                        column: x => x.VehicleID,
                        principalSchema: "dbo",
                        principalTable: "Vehicles",
                        principalColumn: "VehicleID");
                });

            migrationBuilder.CreateTable(
                name: "BookingServices",
                schema: "dbo",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServices", x => new { x.BookingID, x.ServiceID });
                    table.ForeignKey(
                        name: "FK_BS_Bookings",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_BS_Services",
                        column: x => x.ServiceID,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "BookingTimeSlots",
                schema: "dbo",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    SlotID = table.Column<int>(type: "int", nullable: false),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    SlotOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingTimeSlots", x => new { x.BookingID, x.SlotID });
                    table.ForeignKey(
                        name: "FK_BTS_Bookings",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_BTS_Technicians",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID");
                    table.ForeignKey(
                        name: "FK_BTS_TimeSlots",
                        column: x => x.SlotID,
                        principalSchema: "dbo",
                        principalTable: "TimeSlots",
                        principalColumn: "SlotID");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                schema: "dbo",
                columns: table => new
                {
                    WorkOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "NOT_STARTED"),
                    StartTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ActualDuration = table.Column<int>(type: "int", nullable: true),
                    InitialMileage = table.Column<int>(type: "int", nullable: true),
                    FinalMileage = table.Column<int>(type: "int", nullable: true),
                    CustomerComplaints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkPerformed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WorkOrde__AE75517563E7EB82", x => x.WorkOrderID);
                    table.ForeignKey(
                        name: "FK_WO_Bookings",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_WO_Technicians",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "dbo",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WorkOrderID = table.Column<int>(type: "int", nullable: false),
                    CustomerID = table.Column<int>(type: "int", nullable: true),
                    BillingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "Guest"),
                    BillingPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    NormalizedBillingPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, computedColumnSql: "(left(replace(replace(replace(replace(isnull([BillingPhone],N''),N' ',N''),N'-',N''),N'(',N''),N')',N''),(20)))", stored: true),
                    InvoiceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "STANDARD"),
                    ParentInvoiceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Invoices__D796AAD52C746B3F", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Invoices_WorkOrders",
                        column: x => x.WorkOrderID,
                        principalSchema: "dbo",
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderID");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklists",
                schema: "dbo",
                columns: table => new
                {
                    ChecklistID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__4C1D49BAAA52170A", x => x.ChecklistID);
                    table.ForeignKey(
                        name: "FK__Maintenan__WorkO__54B68676",
                        column: x => x.WorkOrderID,
                        principalSchema: "dbo",
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderID");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderChargeProposals",
                schema: "dbo",
                columns: table => new
                {
                    ProposalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WorkOrde__6F39E100BDB375C0", x => x.ProposalID);
                    table.ForeignKey(
                        name: "FK__WorkOrder__WorkO__200DB40D",
                        column: x => x.WorkOrderID,
                        principalSchema: "dbo",
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderID");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderParts",
                schema: "dbo",
                columns: table => new
                {
                    WorkOrderID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    QuantityUsed = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderParts", x => new { x.WorkOrderID, x.PartID });
                    table.ForeignKey(
                        name: "FK_WOP_Parts",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK_WOP_WorkOrders",
                        column: x => x.WorkOrderID,
                        principalSchema: "dbo",
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderID");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                schema: "dbo",
                columns: table => new
                {
                    InvoiceItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__InvoiceI__478FE0FC9644E7D3", x => x.InvoiceItemID);
                    table.ForeignKey(
                        name: "FK__InvoiceIt__Invoi__30441BD6",
                        column: x => x.InvoiceID,
                        principalSchema: "dbo",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID");
                    table.ForeignKey(
                        name: "FK__InvoiceIt__PartI__3138400F",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "dbo",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    PayOSOrderCode = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    BuyerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BuyerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BuyerAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    PaidAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payments__9B556A58C7B37C95", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices",
                        column: x => x.InvoiceID,
                        principalSchema: "dbo",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID");
                });

            migrationBuilder.CreateTable(
                name: "UserPromotions",
                schema: "dbo",
                columns: table => new
                {
                    UserPromotionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    PromotionID = table.Column<int>(type: "int", nullable: false),
                    InvoiceID = table.Column<int>(type: "int", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "USED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPromotions", x => x.UserPromotionID);
                    table.ForeignKey(
                        name: "FK_UserPromotions_Customers",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Invoices",
                        column: x => x.InvoiceID,
                        principalSchema: "dbo",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Promotions",
                        column: x => x.PromotionID,
                        principalSchema: "dbo",
                        principalTable: "Promotions",
                        principalColumn: "PromotionID");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistResults",
                schema: "dbo",
                columns: table => new
                {
                    ChecklistID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    Performed = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistResults", x => new { x.ChecklistID, x.ItemID });
                    table.ForeignKey(
                        name: "FK__Maintenan__Check__5887175A",
                        column: x => x.ChecklistID,
                        principalSchema: "dbo",
                        principalTable: "MaintenanceChecklists",
                        principalColumn: "ChecklistID");
                    table.ForeignKey(
                        name: "FK__Maintenan__ItemI__597B3B93",
                        column: x => x.ItemID,
                        principalSchema: "dbo",
                        principalTable: "MaintenanceChecklistItems",
                        principalColumn: "ItemID");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderChargeProposalItems",
                schema: "dbo",
                columns: table => new
                {
                    ProposalItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProposalID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WOCPItems", x => x.ProposalItemID);
                    table.ForeignKey(
                        name: "FK__WorkOrder__PartI__25C68D63",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK__WorkOrder__Propo__24D2692A",
                        column: x => x.ProposalID,
                        principalSchema: "dbo",
                        principalTable: "WorkOrderChargeProposals",
                        principalColumn: "ProposalID");
                });

            migrationBuilder.CreateTable(
                name: "InvoicePayments",
                schema: "dbo",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    PaymentID = table.Column<int>(type: "int", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePayments", x => new { x.InvoiceID, x.PaymentID });
                    table.ForeignKey(
                        name: "FK__InvoicePa__Invoi__2B7F66B9",
                        column: x => x.InvoiceID,
                        principalSchema: "dbo",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID");
                    table.ForeignKey(
                        name: "FK__InvoicePa__Payme__2C738AF2",
                        column: x => x.PaymentID,
                        principalSchema: "dbo",
                        principalTable: "Payments",
                        principalColumn: "PaymentID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CenterID",
                schema: "dbo",
                table: "Bookings",
                column: "CenterID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerID",
                schema: "dbo",
                table: "Bookings",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_EndSlotID",
                schema: "dbo",
                table: "Bookings",
                column: "EndSlotID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StartSlotID",
                schema: "dbo",
                table: "Bookings",
                column: "StartSlotID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleID",
                schema: "dbo",
                table: "Bookings",
                column: "VehicleID");

            migrationBuilder.CreateIndex(
                name: "UQ__Bookings__C6E56BD5C752B9AD",
                schema: "dbo",
                table: "Bookings",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_ServiceID",
                schema: "dbo",
                table: "BookingServices",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTimeSlots_SlotID",
                schema: "dbo",
                table: "BookingTimeSlots",
                column: "SlotID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTimeSlots_TechnicianID",
                schema: "dbo",
                table: "BookingTimeSlots",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "UQ_Channels_Code",
                schema: "dbo",
                table: "Channels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Customer__066785211CB53DFC",
                schema: "dbo",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Customers_GuestPhone",
                schema: "dbo",
                table: "Customers",
                column: "NormalizedPhone",
                unique: true,
                filter: "([IsGuest]=(1) AND [NormalizedPhone] IS NOT NULL AND [NormalizedPhone]<>N'')");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_UserID_NotNull",
                schema: "dbo",
                table: "Customers",
                column: "UserID",
                unique: true,
                filter: "([UserID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_PartID",
                schema: "dbo",
                table: "Inventory",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "UQ_Inv_CenterPart",
                schema: "dbo",
                table: "Inventory",
                columns: new[] { "CenterID", "PartID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_Part",
                schema: "dbo",
                table: "InventoryBalances",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_Warehouse",
                schema: "dbo",
                table: "InventoryBalances",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseID",
                schema: "dbo",
                table: "InventoryTransactions",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_InvTrans_CreatedAt",
                schema: "dbo",
                table: "InventoryTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvTrans_Part_Warehouse",
                schema: "dbo",
                table: "InventoryTransactions",
                columns: new[] { "PartID", "WarehouseID" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransferItems_PartID",
                schema: "dbo",
                table: "InventoryTransferItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_InvTransfers_FromWh",
                schema: "dbo",
                table: "InventoryTransfers",
                column: "FromWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_InvTransfers_Status",
                schema: "dbo",
                table: "InventoryTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvTransfers_ToWh",
                schema: "dbo",
                table: "InventoryTransfers",
                column: "ToWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceID",
                schema: "dbo",
                table: "InvoiceItems",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_PartID",
                schema: "dbo",
                table: "InvoiceItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_PaymentID",
                schema: "dbo",
                table: "InvoicePayments",
                column: "PaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerID",
                schema: "dbo",
                table: "Invoices",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceType",
                schema: "dbo",
                table: "Invoices",
                column: "InvoiceType");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_NormPhone",
                schema: "dbo",
                table: "Invoices",
                column: "NormalizedBillingPhone");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Parent",
                schema: "dbo",
                table: "Invoices",
                column: "ParentInvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                schema: "dbo",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_WorkOrderID",
                schema: "dbo",
                table: "Invoices",
                column: "WorkOrderID");

            migrationBuilder.CreateIndex(
                name: "UQ__Invoices__D776E9818A87403A",
                schema: "dbo",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApprovedBy",
                schema: "dbo",
                table: "LeaveRequests",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_TechnicianID",
                schema: "dbo",
                table: "LeaveRequests",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistResults_ItemID",
                schema: "dbo",
                table: "MaintenanceChecklistResults",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklists_WorkOrderID",
                schema: "dbo",
                table: "MaintenanceChecklists",
                column: "WorkOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_VehicleID",
                schema: "dbo",
                table: "MaintenanceReminders",
                column: "VehicleID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                schema: "dbo",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPCodes_UserID",
                schema: "dbo",
                table: "OTPCodes",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Parts__025D30D9F2F3C21F",
                schema: "dbo",
                table: "Parts",
                column: "PartNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Created",
                schema: "dbo",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceID",
                schema: "dbo",
                table: "Payments",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                schema: "dbo",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ__Payments__0426C86F4FABC463",
                schema: "dbo",
                table: "Payments",
                column: "PayOSOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Payments__106D3BA8E41F72D2",
                schema: "dbo",
                table: "Payments",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_ActiveDate",
                schema: "dbo",
                table: "Promotions",
                columns: new[] { "StartDate", "EndDate" },
                filter: "([Status]=N'ACTIVE')");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StatusDates",
                schema: "dbo",
                table: "Promotions",
                columns: new[] { "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_Promotions_Code",
                schema: "dbo",
                table: "Promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_Part",
                schema: "dbo",
                table: "SalesOrderItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CenterID",
                schema: "dbo",
                table: "SalesOrders",
                column: "CenterID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Channel",
                schema: "dbo",
                table: "SalesOrders",
                column: "ChannelID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerID",
                schema: "dbo",
                table: "SalesOrders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Status",
                schema: "dbo",
                table: "SalesOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Warehouse",
                schema: "dbo",
                table: "SalesOrders",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_Parent",
                schema: "dbo",
                table: "ServiceCategories",
                column: "ParentCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_SPI_Package",
                schema: "dbo",
                table: "ServicePackageItems",
                columns: new[] { "PackageID", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SPI_Service",
                schema: "dbo",
                table: "ServicePackageItems",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "UQ__ServiceP__94185429C3E364E2",
                schema: "dbo",
                table: "ServicePackages",
                column: "PackageCode",
                unique: true,
                filter: "[PackageCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryID",
                schema: "dbo",
                table: "Services",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_CenterID",
                schema: "dbo",
                table: "Staff",
                column: "CenterID");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_UserID",
                schema: "dbo",
                table: "Staff",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Staff__D83AD812AE8FD68C",
                schema: "dbo",
                table: "Staff",
                column: "StaffCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_CenterID",
                schema: "dbo",
                table: "Technicians",
                column: "CenterID");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_UserID",
                schema: "dbo",
                table: "Technicians",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Technici__ED64BD1AD2C78212",
                schema: "dbo",
                table: "Technicians",
                column: "TechnicianCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTimeSlots_SlotID",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                column: "SlotID");

            migrationBuilder.CreateIndex(
                name: "IX_TTS_DateSlot",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                columns: new[] { "WorkDate", "SlotID" });

            migrationBuilder.CreateIndex(
                name: "UX_TTS_TechDateSlot",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                columns: new[] { "TechnicianID", "WorkDate", "SlotID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__TimeSlot__488B1607F122A36A",
                schema: "dbo",
                table: "TimeSlots",
                column: "SlotTime",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_Customer",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "CustomerID", "UsedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_InvoiceID",
                schema: "dbo",
                table: "UserPromotions",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_Promotion",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "PromotionID", "UsedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_UserPromotions_Promo_Invoice",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "PromotionID", "InvoiceID" },
                unique: true,
                filter: "([InvoiceID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D105349C971121",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CustomerID",
                schema: "dbo",
                table: "Vehicles",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ModelID",
                schema: "dbo",
                table: "Vehicles",
                column: "ModelID");

            migrationBuilder.CreateIndex(
                name: "UQ__Vehicles__026BC15C5D38E10E",
                schema: "dbo",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Vehicles__C5DF234C3C1DD5D3",
                schema: "dbo",
                table: "Vehicles",
                column: "VIN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Center",
                schema: "dbo",
                table: "Warehouses",
                column: "CenterID");

            migrationBuilder.CreateIndex(
                name: "UQ_Warehouses_Center_Code",
                schema: "dbo",
                table: "Warehouses",
                columns: new[] { "CenterID", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChargeProposalItems_PartID",
                schema: "dbo",
                table: "WorkOrderChargeProposalItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "UX_WOCPItems_Proposal_Part_NotNull",
                schema: "dbo",
                table: "WorkOrderChargeProposalItems",
                columns: new[] { "ProposalID", "PartID" },
                unique: true,
                filter: "([PartID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderChargeProposals_WorkOrderID",
                schema: "dbo",
                table: "WorkOrderChargeProposals",
                column: "WorkOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_PartID",
                schema: "dbo",
                table: "WorkOrderParts",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_BookingID",
                schema: "dbo",
                table: "WorkOrders",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TechnicianID",
                schema: "dbo",
                table: "WorkOrders",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "UQ__WorkOrde__1FA44F96DE057B33",
                schema: "dbo",
                table: "WorkOrders",
                column: "WorkOrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingServices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BookingTimeSlots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Inventory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InventoryBalances",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InventoryTransactions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InventoryTransferItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InvoiceItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InvoicePayments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistResults",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaintenanceReminders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OTPCodes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SalesOrderItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServicePackageItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TechnicianTimeSlots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserPromotions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkOrderChargeProposalItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkOrderParts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InventoryTransfers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklists",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServicePackages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkOrderChargeProposals",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Parts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Channels",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkOrders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Technicians",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TimeSlots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Vehicles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceCenters",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VehicleModels",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "Seq_InvoiceNumber",
                schema: "dbo");
        }
    }
}
