using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basket.Data.Migrations
{
    /// <inheritdoc />
    public partial class DurableAuditAndMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "basket",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                schema: "basket",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AuditRecords",
                schema: "basket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditDeliveries",
                schema: "basket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditDeliveries_AuditRecords_Id",
                        column: x => x.Id,
                        principalSchema: "basket",
                        principalTable: "AuditRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn_NextAttemptAt",
                schema: "basket",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOn", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditDeliveries_ExportedAtUtc",
                schema: "basket",
                table: "AuditDeliveries",
                column: "ExportedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_TenantId_OccurredAtUtc",
                schema: "basket",
                table: "AuditRecords",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditDeliveries",
                schema: "basket");

            migrationBuilder.DropTable(
                name: "AuditRecords",
                schema: "basket");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOn_NextAttemptAt",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                schema: "basket",
                table: "OutboxMessages");
        }
    }
}
