using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullEntityAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE catalog."Products" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                ALTER TABLE catalog."Products" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE catalog."Products" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "catalog",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "catalog",
                table: "Products",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("ALTER TABLE catalog.\"Products\" ALTER COLUMN \"CreatedAt\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE catalog."Products" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE catalog."Products" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                """);
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "catalog",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "catalog",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "catalog",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
