using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedPaymentMethods_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_SavedAddresses_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "accounts",
                table: "SavedPaymentMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "accounts",
                table: "SavedAddresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "accounts",
                table: "CustomerAccounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE accounts."CustomerAccounts"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111', "UserId" = "Id";
                UPDATE accounts."SavedAddresses"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                UPDATE accounts."SavedPaymentMethods"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "accounts", table: "SavedPaymentMethods",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "accounts", table: "SavedAddresses",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "accounts", table: "CustomerAccounts",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId", schema: "accounts", table: "CustomerAccounts",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedPaymentMethods_CustomerAccountId",
                schema: "accounts",
                table: "SavedPaymentMethods",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPaymentMethods_TenantId_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedPaymentMethods",
                columns: new[] { "TenantId", "CustomerAccountId", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_CustomerAccountId",
                schema: "accounts",
                table: "SavedAddresses",
                column: "CustomerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_TenantId_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedAddresses",
                columns: new[] { "TenantId", "CustomerAccountId", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_TenantId_UserId",
                schema: "accounts",
                table: "CustomerAccounts",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedPaymentMethods_CustomerAccountId",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_SavedPaymentMethods_TenantId_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_SavedAddresses_CustomerAccountId",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.DropIndex(
                name: "IX_SavedAddresses_TenantId_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccounts_TenantId_UserId",
                schema: "accounts",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "accounts",
                table: "SavedPaymentMethods");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "accounts",
                table: "SavedAddresses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "accounts",
                table: "CustomerAccounts");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "accounts",
                table: "CustomerAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPaymentMethods_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedPaymentMethods",
                columns: new[] { "CustomerAccountId", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAddresses_CustomerAccountId_Label",
                schema: "accounts",
                table: "SavedAddresses",
                columns: new[] { "CustomerAccountId", "Label" });
        }
    }
}
