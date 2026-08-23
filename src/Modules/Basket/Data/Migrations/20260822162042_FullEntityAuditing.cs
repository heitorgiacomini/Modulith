using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Basket.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullEntityAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE basket."ShoppingCarts" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                UPDATE basket."ShoppingCartItems" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                UPDATE basket."OutboxMessages" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                ALTER TABLE basket."ShoppingCarts" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE basket."ShoppingCarts" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                ALTER TABLE basket."ShoppingCartItems" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE basket."ShoppingCartItems" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                ALTER TABLE basket."OutboxMessages" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE basket."OutboxMessages" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                """);
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "ShoppingCarts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "basket",
                table: "ShoppingCarts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "basket",
                table: "ShoppingCarts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "basket",
                table: "ShoppingCarts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "basket",
                table: "ShoppingCarts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCarts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "boolean",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "basket",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts",
                column: "UserName",
                unique: true,
                filter: "\"IsDeleted\" IS NOT TRUE");

            migrationBuilder.Sql("""
                ALTER TABLE basket."ShoppingCarts" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                ALTER TABLE basket."ShoppingCartItems" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                ALTER TABLE basket."OutboxMessages" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE basket."ShoppingCarts" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE basket."ShoppingCarts" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                ALTER TABLE basket."ShoppingCartItems" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE basket."ShoppingCartItems" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                ALTER TABLE basket."OutboxMessages" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE basket."OutboxMessages" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                """);
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "basket",
                table: "ShoppingCartItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "basket",
                table: "ShoppingCartItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "basket",
                table: "ShoppingCartItems");

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

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "ShoppingCarts",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "basket",
                table: "ShoppingCarts",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "basket",
                table: "ShoppingCarts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "basket",
                table: "ShoppingCartItems",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "basket",
                table: "OutboxMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "basket",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserName",
                schema: "basket",
                table: "ShoppingCarts",
                column: "UserName",
                unique: true);
        }
    }
}
