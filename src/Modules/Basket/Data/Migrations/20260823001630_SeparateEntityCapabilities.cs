using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basket.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateEntityCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE basket."ShoppingCarts" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                UPDATE basket."ShoppingCartItems" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastModified",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                schema: "basket",
                table: "OutboxMessages");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCarts",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts",
                column: "UserName",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCarts",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "basket",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "basket",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "OutboxMessages",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                schema: "basket",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts",
                column: "UserName",
                unique: true,
                filter: "\"IsDeleted\" IS NOT TRUE");
        }
    }
}
