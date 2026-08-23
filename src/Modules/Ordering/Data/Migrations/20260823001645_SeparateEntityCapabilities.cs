using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateEntityCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE ordering."Orders" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                UPDATE ordering."OrderItems" SET "IsDeleted" = FALSE WHERE "IsDeleted" IS NULL;
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "ordering",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "ordering",
                table: "OrderItems",
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
                schema: "ordering",
                table: "Orders",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "ordering",
                table: "OrderItems",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
