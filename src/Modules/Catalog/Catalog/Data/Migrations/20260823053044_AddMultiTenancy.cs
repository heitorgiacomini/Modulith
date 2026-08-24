using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE catalog."Products"
                SET "TenantId" = '11111111-1111-1111-1111-111111111111';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId", schema: "catalog", table: "Products",
                type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "catalog",
                table: "Products");
        }
    }
}
