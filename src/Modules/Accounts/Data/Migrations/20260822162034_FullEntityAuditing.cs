using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullEntityAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE accounts."SavedPaymentMethods" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                UPDATE accounts."SavedAddresses" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                UPDATE accounts."CustomerAccounts" SET "CreatedAt" = CURRENT_TIMESTAMP WHERE "CreatedAt" IS NULL;
                ALTER TABLE accounts."SavedPaymentMethods" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE accounts."SavedPaymentMethods" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                ALTER TABLE accounts."SavedAddresses" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE accounts."SavedAddresses" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                ALTER TABLE accounts."CustomerAccounts" ALTER COLUMN "CreatedBy" TYPE uuid USING CASE WHEN "CreatedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "CreatedBy"::uuid ELSE NULL END;
                ALTER TABLE accounts."CustomerAccounts" ALTER COLUMN "LastModifiedBy" TYPE uuid USING CASE WHEN "LastModifiedBy" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN "LastModifiedBy"::uuid ELSE NULL END;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "boolean",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "accounts",
                table: "SavedAddresses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "accounts",
                table: "SavedAddresses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "accounts",
                table: "SavedAddresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "accounts",
                table: "SavedAddresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "accounts",
                table: "SavedAddresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedAddresses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LastModifiedBy",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedBy",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE accounts."SavedPaymentMethods" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                ALTER TABLE accounts."SavedAddresses" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                ALTER TABLE accounts."CustomerAccounts" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE accounts."SavedPaymentMethods" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE accounts."SavedPaymentMethods" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                ALTER TABLE accounts."SavedAddresses" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE accounts."SavedAddresses" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                ALTER TABLE accounts."CustomerAccounts" ALTER COLUMN "CreatedBy" TYPE text USING "CreatedBy"::text;
                ALTER TABLE accounts."CustomerAccounts" ALTER COLUMN "LastModifiedBy" TYPE text USING "LastModifiedBy"::text;
                """);
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "accounts",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "accounts",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "accounts",
                table: "CustomerAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "accounts",
                table: "SavedAddresses",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "accounts",
                table: "SavedAddresses",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "accounts",
                table: "SavedAddresses",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
