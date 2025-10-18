using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVServiceCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "PartCategories",
                schema: "dbo",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartCategories", x => x.CategoryId);
                    table.ForeignKey(
                        name: "FK_PartCategories_PartCategories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "dbo",
                        principalTable: "PartCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
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
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.PartID);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "dbo",
                columns: table => new
                {
                    PromotionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    UsageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.PromotionID);
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
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCenters", x => x.CenterID);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "dbo",
                columns: table => new
                {
                    ServiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.ServiceID);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "dbo",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                schema: "dbo",
                columns: table => new
                {
                    SlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlotTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.SlotID);
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
                    RefreshToken = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModel",
                schema: "dbo",
                columns: table => new
                {
                    ModelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModel", x => x.ModelID);
                });

            migrationBuilder.CreateTable(
                name: "PartCategoryMaps",
                schema: "dbo",
                columns: table => new
                {
                    PartId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartCategoryMaps", x => new { x.PartId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PartCategoryMaps_PartCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "PartCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartCategoryMaps_Parts_PartId",
                        column: x => x.PartId,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                schema: "dbo",
                columns: table => new
                {
                    InventoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.InventoryID);
                    table.ForeignKey(
                        name: "FK_Inventory_ServiceCenters_CenterID",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceChecklistTemplates",
                schema: "dbo",
                columns: table => new
                {
                    TemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceChecklistTemplates", x => x.TemplateID);
                    table.ForeignKey(
                        name: "FK_ServiceChecklistTemplates_Services_ServiceID",
                        column: x => x.ServiceID,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                schema: "dbo",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PackageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    TotalCredits = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.PackageId);
                    table.CheckConstraint("CK_ServicePackages_DiscountPercent", "DiscountPercent >= 0 AND DiscountPercent <= 100");
                    table.CheckConstraint("CK_ServicePackages_Price", "Price >= 0");
                    table.CheckConstraint("CK_ServicePackages_TotalCredits", "TotalCredits > 0");
                    table.CheckConstraint("CK_ServicePackages_ValidDates", "ValidFrom IS NULL OR ValidTo IS NULL OR ValidFrom <= ValidTo");
                    table.ForeignKey(
                        name: "FK_ServicePackages_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "dbo",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    IsGuest = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_Customers_Users_UserID",
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
                    ReadAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserID",
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
                    table.PrimaryKey("PK_OTPCodes", x => x.OTPID);
                    table.ForeignKey(
                        name: "FK_OTPCodes_Users_UserID",
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.StaffID);
                    table.ForeignKey(
                        name: "FK_Staff_ServiceCenters_CenterID",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Staff_Users_UserID",
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
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.TechnicianID);
                    table.ForeignKey(
                        name: "FK_Technicians_ServiceCenters_CenterID",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Technicians_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "VehicleModelParts",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModelParts", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VehicleModelParts_Parts_PartID",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleModelParts_VehicleModel_ModelID",
                        column: x => x.ModelID,
                        principalSchema: "dbo",
                        principalTable: "VehicleModel",
                        principalColumn: "ModelID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryParts",
                schema: "dbo",
                columns: table => new
                {
                    InventoryPartID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryParts", x => x.InventoryPartID);
                    table.ForeignKey(
                        name: "FK_InventoryParts_Inventory_InventoryID",
                        column: x => x.InventoryID,
                        principalSchema: "dbo",
                        principalTable: "Inventory",
                        principalColumn: "InventoryID");
                    table.ForeignKey(
                        name: "FK_InventoryParts_Parts_PartID",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "ServiceChecklistTemplateItems",
                schema: "dbo",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceChecklistTemplateItems", x => x.ItemID);
                    table.ForeignKey(
                        name: "FK_ServiceChecklistTemplateItems_Parts_PartID",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceChecklistTemplateItems_ServiceChecklistTemplates_TemplateID",
                        column: x => x.TemplateID,
                        principalSchema: "dbo",
                        principalTable: "ServiceChecklistTemplates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerServiceCredits",
                schema: "dbo",
                columns: table => new
                {
                    CreditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    TotalCredits = table.Column<int>(type: "int", nullable: false),
                    UsedCredits = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerServiceCredits", x => x.CreditId);
                    table.CheckConstraint("CK_CustomerServiceCredits_ExpiryDate", "ExpiryDate IS NULL OR ExpiryDate >= PurchaseDate");
                    table.CheckConstraint("CK_CustomerServiceCredits_Status", "Status IN ('ACTIVE', 'EXPIRED', 'USED_UP')");
                    table.CheckConstraint("CK_CustomerServiceCredits_TotalCredits", "TotalCredits > 0");
                    table.CheckConstraint("CK_CustomerServiceCredits_UsedCredits", "UsedCredits >= 0 AND UsedCredits <= TotalCredits");
                    table.ForeignKey(
                        name: "FK_CustomerServiceCredits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerServiceCredits_ServicePackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "ServicePackages",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerServiceCredits_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "dbo",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "PENDING"),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                schema: "dbo",
                columns: table => new
                {
                    VehicleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    VIN = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentMileage = table.Column<int>(type: "int", nullable: false),
                    LastServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    ModelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleID);
                    table.ForeignKey(
                        name: "FK_Vehicles_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleModel_ModelID",
                        column: x => x.ModelID,
                        principalSchema: "dbo",
                        principalTable: "VehicleModel",
                        principalColumn: "ModelID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "dbo",
                columns: table => new
                {
                    OrderItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.OrderItemID);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OrderItems_Parts_PartID",
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
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    VehicleID = table.Column<int>(type: "int", nullable: false),
                    CenterID = table.Column<int>(type: "int", nullable: false),
                    SlotID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "PENDING"),
                    SpecialRequests = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    TechnicianID = table.Column<int>(type: "int", nullable: true),
                    CurrentMileage = table.Column<int>(type: "int", nullable: true),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingID);
                    table.ForeignKey(
                        name: "FK_Bookings_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Bookings_ServiceCenters_CenterID",
                        column: x => x.CenterID,
                        principalSchema: "dbo",
                        principalTable: "ServiceCenters",
                        principalColumn: "CenterID");
                    table.ForeignKey(
                        name: "FK_Bookings_Services_ServiceID",
                        column: x => x.ServiceID,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                    table.ForeignKey(
                        name: "FK_Bookings_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID");
                    table.ForeignKey(
                        name: "FK_Bookings_TimeSlots_SlotID",
                        column: x => x.SlotID,
                        principalSchema: "dbo",
                        principalTable: "TimeSlots",
                        principalColumn: "SlotID");
                    table.ForeignKey(
                        name: "FK_Bookings_Vehicles_VehicleID",
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
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    DueMileage = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceReminders", x => x.ReminderID);
                    table.ForeignKey(
                        name: "FK_MaintenanceReminders_Services_ServiceID",
                        column: x => x.ServiceID,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                    table.ForeignKey(
                        name: "FK_MaintenanceReminders_Vehicles_VehicleID",
                        column: x => x.VehicleID,
                        principalSchema: "dbo",
                        principalTable: "Vehicles",
                        principalColumn: "VehicleID");
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                schema: "dbo",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: true),
                    OrderID = table.Column<int>(type: "int", nullable: true),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    PartID = table.Column<int>(type: "int", nullable: true),
                    TechnicianID = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_Feedbacks_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_Feedbacks_Parts_PartID",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "dbo",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    BookingId = table.Column<int>(type: "int", nullable: true),
                    OrderID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_Invoices_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Invoices_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklists",
                schema: "dbo",
                columns: table => new
                {
                    ChecklistID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklists", x => x.ChecklistID);
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklists_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                });

            migrationBuilder.CreateTable(
                name: "TechnicianTimeSlots",
                schema: "dbo",
                columns: table => new
                {
                    TechnicianSlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianID = table.Column<int>(type: "int", nullable: false),
                    SlotID = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianTimeSlots", x => x.TechnicianSlotID);
                    table.ForeignKey(
                        name: "FK_TechnicianTimeSlots_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_TechnicianTimeSlots_Technicians_TechnicianID",
                        column: x => x.TechnicianID,
                        principalSchema: "dbo",
                        principalTable: "Technicians",
                        principalColumn: "TechnicianID");
                    table.ForeignKey(
                        name: "FK_TechnicianTimeSlots_TimeSlots_SlotID",
                        column: x => x.SlotID,
                        principalSchema: "dbo",
                        principalTable: "TimeSlots",
                        principalColumn: "SlotID");
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
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    OrderID = table.Column<int>(type: "int", nullable: true),
                    ServiceID = table.Column<int>(type: "int", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "USED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPromotions", x => x.UserPromotionID);
                    table.ForeignKey(
                        name: "FK_UserPromotions_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Orders_OrderID",
                        column: x => x.OrderID,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Promotions_PromotionID",
                        column: x => x.PromotionID,
                        principalSchema: "dbo",
                        principalTable: "Promotions",
                        principalColumn: "PromotionID");
                    table.ForeignKey(
                        name: "FK_UserPromotions_Services_ServiceID",
                        column: x => x.ServiceID,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderParts",
                schema: "dbo",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    VehicleModelPartID = table.Column<int>(type: "int", nullable: true),
                    QuantityUsed = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderParts", x => new { x.BookingID, x.PartID });
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalSchema: "dbo",
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_Parts_PartID",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_VehicleModelParts_VehicleModelPartID",
                        column: x => x.VehicleModelPartID,
                        principalSchema: "dbo",
                        principalTable: "VehicleModelParts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
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
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetime())"),
                    PaidAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "PAYOS"),
                    PaidByUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceID",
                        column: x => x.InvoiceID,
                        principalSchema: "dbo",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID");
                    table.ForeignKey(
                        name: "FK_Payments_Users_PaidByUserID",
                        column: x => x.PaidByUserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistResults",
                schema: "dbo",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChecklistID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistResults", x => x.ResultID);
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistResults_MaintenanceChecklists_ChecklistID",
                        column: x => x.ChecklistID,
                        principalSchema: "dbo",
                        principalTable: "MaintenanceChecklists",
                        principalColumn: "ChecklistID");
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistResults_Parts_PartID",
                        column: x => x.PartID,
                        principalSchema: "dbo",
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "ConversationMembers",
                schema: "dbo",
                columns: table => new
                {
                    MemberID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationID = table.Column<long>(type: "bigint", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    GuestSessionID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RoleInConversation = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMembers", x => x.MemberID);
                    table.ForeignKey(
                        name: "FK_ConversationMembers_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "dbo",
                columns: table => new
                {
                    ConversationID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageMessageId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationID);
                    table.ForeignKey(
                        name: "FK_Conversations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "dbo",
                columns: table => new
                {
                    MessageID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationID = table.Column<long>(type: "bigint", nullable: false),
                    SenderUserID = table.Column<int>(type: "int", nullable: true),
                    SenderGuestSessionID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReplyToMessageID = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageID);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationID",
                        column: x => x.ConversationID,
                        principalSchema: "dbo",
                        principalTable: "Conversations",
                        principalColumn: "ConversationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Messages_ReplyToMessageID",
                        column: x => x.ReplyToMessageID,
                        principalSchema: "dbo",
                        principalTable: "Messages",
                        principalColumn: "MessageID");
                    table.ForeignKey(
                        name: "FK_Messages_Users_SenderUserID",
                        column: x => x.SenderUserID,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserID");
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
                name: "IX_Bookings_ServiceID",
                schema: "dbo",
                table: "Bookings",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SlotID",
                schema: "dbo",
                table: "Bookings",
                column: "SlotID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TechnicianID",
                schema: "dbo",
                table: "Bookings",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleID",
                schema: "dbo",
                table: "Bookings",
                column: "VehicleID");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_Guest",
                schema: "dbo",
                table: "ConversationMembers",
                column: "GuestSessionID");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_User",
                schema: "dbo",
                table: "ConversationMembers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UX_ConversationMembers_Conversation_Guest",
                schema: "dbo",
                table: "ConversationMembers",
                columns: new[] { "ConversationID", "GuestSessionID" });

            migrationBuilder.CreateIndex(
                name: "UX_ConversationMembers_Conversation_User",
                schema: "dbo",
                table: "ConversationMembers",
                columns: new[] { "ConversationID", "UserID" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CustomerId",
                schema: "dbo",
                table: "Conversations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAt",
                schema: "dbo",
                table: "Conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageMessageId",
                schema: "dbo",
                table: "Conversations",
                column: "LastMessageMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserID",
                schema: "dbo",
                table: "Customers",
                column: "UserID",
                unique: true,
                filter: "[UserID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceCredits_CustomerId",
                schema: "dbo",
                table: "CustomerServiceCredits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceCredits_CustomerId_ServiceId",
                schema: "dbo",
                table: "CustomerServiceCredits",
                columns: new[] { "CustomerId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceCredits_ExpiryDate",
                schema: "dbo",
                table: "CustomerServiceCredits",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceCredits_PackageId",
                schema: "dbo",
                table: "CustomerServiceCredits",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceCredits_ServiceId",
                schema: "dbo",
                table: "CustomerServiceCredits",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerServiceCredits_Status",
                schema: "dbo",
                table: "CustomerServiceCredits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_BookingID",
                schema: "dbo",
                table: "Feedbacks",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CustomerID",
                schema: "dbo",
                table: "Feedbacks",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_OrderID",
                schema: "dbo",
                table: "Feedbacks",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_PartID",
                schema: "dbo",
                table: "Feedbacks",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_TechnicianID",
                schema: "dbo",
                table: "Feedbacks",
                column: "TechnicianID");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_CenterID",
                schema: "dbo",
                table: "Inventory",
                column: "CenterID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_InventoryID_PartID",
                schema: "dbo",
                table: "InventoryParts",
                columns: new[] { "InventoryID", "PartID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_PartID",
                schema: "dbo",
                table: "InventoryParts",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BookingId",
                schema: "dbo",
                table: "Invoices",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerID",
                schema: "dbo",
                table: "Invoices",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderID",
                schema: "dbo",
                table: "Invoices",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                schema: "dbo",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistResults_ChecklistID",
                schema: "dbo",
                table: "MaintenanceChecklistResults",
                column: "ChecklistID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistResults_PartID",
                schema: "dbo",
                table: "MaintenanceChecklistResults",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklists_BookingID",
                schema: "dbo",
                table: "MaintenanceChecklists",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_ServiceID",
                schema: "dbo",
                table: "MaintenanceReminders",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_VehicleID",
                schema: "dbo",
                table: "MaintenanceReminders",
                column: "VehicleID");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationID_CreatedAt",
                schema: "dbo",
                table: "Messages",
                columns: new[] { "ConversationID", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageID",
                schema: "dbo",
                table: "Messages",
                column: "ReplyToMessageID");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderGuestSessionID_CreatedAt",
                schema: "dbo",
                table: "Messages",
                columns: new[] { "SenderGuestSessionID", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderUserID_CreatedAt",
                schema: "dbo",
                table: "Messages",
                columns: new[] { "SenderUserID", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                schema: "dbo",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderID",
                schema: "dbo",
                table: "OrderItems",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PartID",
                schema: "dbo",
                table: "OrderItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerID",
                schema: "dbo",
                table: "Orders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_OTPCodes_UserID",
                schema: "dbo",
                table: "OTPCodes",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PartCategories_CategoryName",
                schema: "dbo",
                table: "PartCategories",
                column: "CategoryName");

            migrationBuilder.CreateIndex(
                name: "IX_PartCategories_IsActive",
                schema: "dbo",
                table: "PartCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PartCategories_ParentId",
                schema: "dbo",
                table: "PartCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_PartCategoryMaps_CategoryId",
                schema: "dbo",
                table: "PartCategoryMaps",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PartCategoryMaps_IsPrimary",
                schema: "dbo",
                table: "PartCategoryMaps",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_PartCategoryMaps_PartId",
                schema: "dbo",
                table: "PartCategoryMaps",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_PartNumber",
                schema: "dbo",
                table: "Parts",
                column: "PartNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                schema: "dbo",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceID",
                schema: "dbo",
                table: "Payments",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidByUserID",
                schema: "dbo",
                table: "Payments",
                column: "PaidByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentCode",
                schema: "dbo",
                table: "Payments",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                schema: "dbo",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Code",
                schema: "dbo",
                table: "Promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StartDate_EndDate",
                schema: "dbo",
                table: "Promotions",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Status_StartDate_EndDate",
                schema: "dbo",
                table: "Promotions",
                columns: new[] { "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChecklistTemplateItems_PartID",
                schema: "dbo",
                table: "ServiceChecklistTemplateItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChecklistTemplateItems_TemplateID_PartID",
                schema: "dbo",
                table: "ServiceChecklistTemplateItems",
                columns: new[] { "TemplateID", "PartID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceChecklistTemplates_ServiceID",
                schema: "dbo",
                table: "ServiceChecklistTemplates",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_PackageCode",
                schema: "dbo",
                table: "ServicePackages",
                column: "PackageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_ServiceId",
                schema: "dbo",
                table: "ServicePackages",
                column: "ServiceId");

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
                name: "IX_TechnicianTimeSlots_BookingID",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTimeSlots_SlotID",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                column: "SlotID");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTimeSlots_TechnicianID_WorkDate_SlotID",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                columns: new[] { "TechnicianID", "WorkDate", "SlotID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTimeSlots_WorkDate_SlotID",
                schema: "dbo",
                table: "TechnicianTimeSlots",
                columns: new[] { "WorkDate", "SlotID" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_SlotTime",
                schema: "dbo",
                table: "TimeSlots",
                column: "SlotTime",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_BookingID",
                schema: "dbo",
                table: "UserPromotions",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_CustomerID_UsedAt",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "CustomerID", "UsedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_OrderID",
                schema: "dbo",
                table: "UserPromotions",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_PromotionID_BookingID",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "PromotionID", "BookingID" },
                unique: true,
                filter: "[BookingID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_PromotionID_OrderID",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "PromotionID", "OrderID" },
                unique: true,
                filter: "[OrderID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_PromotionID_UsedAt",
                schema: "dbo",
                table: "UserPromotions",
                columns: new[] { "PromotionID", "UsedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserPromotions_ServiceID",
                schema: "dbo",
                table: "UserPromotions",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModel_ModelName",
                schema: "dbo",
                table: "VehicleModel",
                column: "ModelName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelParts_ModelID_PartID",
                schema: "dbo",
                table: "VehicleModelParts",
                columns: new[] { "ModelID", "PartID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModelParts_PartID",
                schema: "dbo",
                table: "VehicleModelParts",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CustomerID",
                schema: "dbo",
                table: "Vehicles",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicensePlate",
                schema: "dbo",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ModelID",
                schema: "dbo",
                table: "Vehicles",
                column: "ModelID");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VIN",
                schema: "dbo",
                table: "Vehicles",
                column: "VIN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_PartID",
                schema: "dbo",
                table: "WorkOrderParts",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_VehicleModelPartID",
                schema: "dbo",
                table: "WorkOrderParts",
                column: "VehicleModelPartID");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMembers_Conversations_ConversationID",
                schema: "dbo",
                table: "ConversationMembers",
                column: "ConversationID",
                principalSchema: "dbo",
                principalTable: "Conversations",
                principalColumn: "ConversationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Messages_LastMessageMessageId",
                schema: "dbo",
                table: "Conversations",
                column: "LastMessageMessageId",
                principalSchema: "dbo",
                principalTable: "Messages",
                principalColumn: "MessageID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Customers_CustomerId",
                schema: "dbo",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conversations_ConversationID",
                schema: "dbo",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "ConversationMembers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CustomerServiceCredits",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Feedbacks",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "InventoryParts",
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
                name: "OrderItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OTPCodes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PartCategoryMaps",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceChecklistTemplateItems",
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
                name: "WorkOrderParts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServicePackages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Inventory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklists",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PartCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceChecklistTemplates",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VehicleModelParts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Parts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Services",
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
                name: "VehicleModel",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");
        }
    }
}
