using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullEntityAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ordering."Orders" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                UPDATE ordering."OrderItems" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                ALTER TABLE ordering."Orders" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE ordering."Orders" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                ALTER TABLE ordering."OrderItems" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE ordering."OrderItems" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "ordering",
                table: "Orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "ordering",
                table: "Orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ordering",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "ordering",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "ordering",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "ordering",
                table: "Orders",
                type: "boolean",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "ordering",
                table: "OrderItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "ordering",
                table: "OrderItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ordering",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "ordering",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "ordering",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "ordering",
                table: "OrderItems",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE ordering."Orders" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                ALTER TABLE ordering."OrderItems" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE ordering."Orders" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE ordering."Orders" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                ALTER TABLE ordering."OrderItems" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE ordering."OrderItems" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                """);
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "ordering",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "ordering",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "ordering",
                table: "OrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "ordering",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "ordering",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "ordering",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "ordering",
                table: "OrderItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "ordering",
                table: "OrderItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "ordering",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
