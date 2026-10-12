using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class DurableAuditAndCheckoutReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRecords",
                schema: "ordering",
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
                name: "CheckoutReceipts",
                schema: "ordering",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutReceipts", x => new { x.TenantId, x.EventId });
                });

            migrationBuilder.CreateTable(
                name: "AuditDeliveries",
                schema: "ordering",
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
                        principalSchema: "ordering",
                        principalTable: "AuditRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditDeliveries_ExportedAtUtc",
                schema: "ordering",
                table: "AuditDeliveries",
                column: "ExportedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_TenantId_OccurredAtUtc",
                schema: "ordering",
                table: "AuditRecords",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditDeliveries",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "CheckoutReceipts",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "AuditRecords",
                schema: "ordering");
        }
    }
}
