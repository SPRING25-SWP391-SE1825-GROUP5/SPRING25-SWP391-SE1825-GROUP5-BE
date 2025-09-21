using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EVServiceCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLocationIdToCenterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeeklySchedule_ServiceCenters_LocationID",
                schema: "ksf00691_team03",
                table: "WeeklySchedule");

            migrationBuilder.RenameTable(
                name: "WeeklySchedule",
                schema: "ksf00691_team03",
                newName: "WeeklySchedule",
                newSchema: "dbo");

            migrationBuilder.RenameColumn(
                name: "LocationID",
                schema: "dbo",
                table: "WeeklySchedule",
                newName: "CenterID");

            migrationBuilder.RenameIndex(
                name: "IX_WeeklySchedule_LocationID",
                schema: "dbo",
                table: "WeeklySchedule",
                newName: "IX_WeeklySchedule_CenterID");

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklySchedule_ServiceCenters_CenterID",
                schema: "dbo",
                table: "WeeklySchedule",
                column: "CenterID",
                principalSchema: "dbo",
                principalTable: "ServiceCenters",
                principalColumn: "CenterID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeeklySchedule_ServiceCenters_CenterID",
                schema: "dbo",
                table: "WeeklySchedule");

            migrationBuilder.EnsureSchema(
                name: "ksf00691_team03");

            migrationBuilder.RenameTable(
                name: "WeeklySchedule",
                schema: "dbo",
                newName: "WeeklySchedule",
                newSchema: "ksf00691_team03");

            migrationBuilder.RenameColumn(
                name: "CenterID",
                schema: "ksf00691_team03",
                table: "WeeklySchedule",
                newName: "LocationID");

            migrationBuilder.RenameIndex(
                name: "IX_WeeklySchedule_CenterID",
                schema: "ksf00691_team03",
                table: "WeeklySchedule",
                newName: "IX_WeeklySchedule_LocationID");

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklySchedule_ServiceCenters_LocationID",
                schema: "ksf00691_team03",
                table: "WeeklySchedule",
                column: "LocationID",
                principalSchema: "dbo",
                principalTable: "ServiceCenters",
                principalColumn: "CenterID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
