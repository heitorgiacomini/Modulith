using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderName",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "ordering",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "ordering",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ordering."Orders"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                UPDATE ordering."OrderItems"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "ordering", table: "Orders",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "ordering", table: "OrderItems",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_OrderName",
                schema: "ordering",
                table: "Orders",
                columns: new[] { "TenantId", "OrderName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_OrderName",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "ordering",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderName",
                schema: "ordering",
                table: "Orders",
                column: "OrderName",
                unique: true);
        }
    }
}
