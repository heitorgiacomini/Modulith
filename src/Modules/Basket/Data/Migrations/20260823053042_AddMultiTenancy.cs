using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basket.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "basket",
                table: "ShoppingCarts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "basket",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE basket."ShoppingCarts"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                UPDATE basket."ShoppingCartItems"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                UPDATE basket."OutboxMessages"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "basket", table: "ShoppingCarts",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "basket", table: "ShoppingCartItems",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "basket", table: "OutboxMessages",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_TenantId_UserName",
                schema: "basket",
                table: "ShoppingCarts",
                columns: new[] { "TenantId", "UserName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_TenantId_UserName",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "basket",
                table: "ShoppingCartItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts",
                column: "UserName",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
