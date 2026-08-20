using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecureCheckoutSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress_Country",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_CVV",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_CardName",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_CardNumber",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_PaymentMethod",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_Country",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ShippingAddress_ZipCode",
                schema: "ordering",
                table: "Orders",
                newName: "ShippingAddress_PostalCode");

            migrationBuilder.RenameColumn(
                name: "ShippingAddress_AddressLine",
                schema: "ordering",
                table: "Orders",
                newName: "ShippingAddress_AddressLine1");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_ZipCode",
                schema: "ordering",
                table: "Orders",
                newName: "BillingAddress_PostalCode");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_AddressLine",
                schema: "ordering",
                table: "Orders",
                newName: "BillingAddress_AddressLine1");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_AddressLine2",
                schema: "ordering",
                table: "Orders",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_City",
                schema: "ordering",
                table: "Orders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_CountryCode",
                schema: "ordering",
                table: "Orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Phone",
                schema: "ordering",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_Brand",
                schema: "ordering",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_CardholderName",
                schema: "ordering",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_Last4",
                schema: "ordering",
                table: "Orders",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_Token",
                schema: "ordering",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_AddressLine2",
                schema: "ordering",
                table: "Orders",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_City",
                schema: "ordering",
                table: "Orders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_CountryCode",
                schema: "ordering",
                table: "Orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_Phone",
                schema: "ordering",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress_AddressLine2",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddress_City",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddress_CountryCode",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BillingAddress_Phone",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_Brand",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_CardholderName",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_Last4",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Payment_Token",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_AddressLine2",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_City",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_CountryCode",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress_Phone",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ShippingAddress_PostalCode",
                schema: "ordering",
                table: "Orders",
                newName: "ShippingAddress_ZipCode");

            migrationBuilder.RenameColumn(
                name: "ShippingAddress_AddressLine1",
                schema: "ordering",
                table: "Orders",
                newName: "ShippingAddress_AddressLine");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_PostalCode",
                schema: "ordering",
                table: "Orders",
                newName: "BillingAddress_ZipCode");

            migrationBuilder.RenameColumn(
                name: "BillingAddress_AddressLine1",
                schema: "ordering",
                table: "Orders",
                newName: "BillingAddress_AddressLine");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress_Country",
                schema: "ordering",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_CVV",
                schema: "ordering",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_CardName",
                schema: "ordering",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payment_CardNumber",
                schema: "ordering",
                table: "Orders",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Payment_PaymentMethod",
                schema: "ordering",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress_Country",
                schema: "ordering",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
