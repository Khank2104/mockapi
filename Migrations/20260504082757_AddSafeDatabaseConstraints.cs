using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSafeDatabaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomServiceSettings_RoomId",
                table: "RoomServiceSettings");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_MotelId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_RoomId",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_RoomId",
                table: "Contracts");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceCode",
                table: "Services",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomServiceSettings_RoomId_ServiceId",
                table: "RoomServiceSettings",
                columns: new[] { "RoomId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_MotelId_RoomCode",
                table: "Rooms",
                columns: new[] { "MotelId", "RoomCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_RoomId_ServiceId_BillingMonth_BillingYear",
                table: "MeterReadings",
                columns: new[] { "RoomId", "ServiceId", "BillingMonth", "BillingYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_RoomId",
                table: "Contracts",
                column: "RoomId",
                unique: true,
                filter: "[ContractStatus] = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_ServiceCode",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_RoomServiceSettings_RoomId_ServiceId",
                table: "RoomServiceSettings");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_MotelId_RoomCode",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_RoomId_ServiceId_BillingMonth_BillingYear",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_RoomId",
                table: "Contracts");

            migrationBuilder.CreateIndex(
                name: "IX_RoomServiceSettings_RoomId",
                table: "RoomServiceSettings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_MotelId",
                table: "Rooms",
                column: "MotelId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_RoomId",
                table: "MeterReadings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_RoomId",
                table: "Contracts",
                column: "RoomId");
        }
    }
}
