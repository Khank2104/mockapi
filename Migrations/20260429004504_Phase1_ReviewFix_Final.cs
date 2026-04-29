using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_ReviewFix_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CreatedBy",
                table: "Services",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoomSettings_CreatedBy",
                table: "RoomSettings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoomServiceSettings_CreatedBy",
                table: "RoomServiceSettings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_HandledBy",
                table: "Requests",
                column: "HandledBy");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivedBy",
                table: "Payments",
                column: "ReceivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_RecordedBy",
                table: "MeterReadings",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedBy",
                table: "Invoices",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CreatedBy",
                table: "Contracts",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_CreatedBy",
                table: "Contracts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedBy",
                table: "Invoices",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeterReadings_Users_RecordedBy",
                table: "MeterReadings",
                column: "RecordedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ReceivedBy",
                table: "Payments",
                column: "ReceivedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_HandledBy",
                table: "Requests",
                column: "HandledBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomServiceSettings_Users_CreatedBy",
                table: "RoomServiceSettings",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomSettings_Users_CreatedBy",
                table: "RoomSettings",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Users_CreatedBy",
                table: "Services",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Users_CreatedBy",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedBy",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MeterReadings_Users_RecordedBy",
                table: "MeterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ReceivedBy",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_HandledBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomServiceSettings_Users_CreatedBy",
                table: "RoomServiceSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomSettings_Users_CreatedBy",
                table: "RoomSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Users_CreatedBy",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedBy",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedBy",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Services_CreatedBy",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_RoomSettings_CreatedBy",
                table: "RoomSettings");

            migrationBuilder.DropIndex(
                name: "IX_RoomServiceSettings_CreatedBy",
                table: "RoomServiceSettings");

            migrationBuilder.DropIndex(
                name: "IX_Requests_HandledBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceivedBy",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_RecordedBy",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CreatedBy",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_CreatedBy",
                table: "Contracts");
        }
    }
}
