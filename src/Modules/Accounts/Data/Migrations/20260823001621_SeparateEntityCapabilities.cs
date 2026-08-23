using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateEntityCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE accounts."SavedPaymentMethods" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                UPDATE accounts."SavedAddresses" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                UPDATE accounts."CustomerAccounts" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedAddresses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "SavedAddresses",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
