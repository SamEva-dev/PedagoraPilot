using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class ExtendTrainingSiteProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "organization",
                table: "training_sites",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "organization",
                table: "training_sites",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Manager",
                schema: "organization",
                table: "training_sites",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "organization",
                table: "training_sites",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "organization",
                table: "training_sites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                schema: "organization",
                table: "training_sites");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "organization",
                table: "training_sites");

            migrationBuilder.DropColumn(
                name: "Manager",
                schema: "organization",
                table: "training_sites");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "organization",
                table: "training_sites");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "organization",
                table: "training_sites");
        }
    }
}
