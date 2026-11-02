using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class EnforceUniqueActiveDirections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_site_direction_one_active_per_organization",
                schema: "organization",
                table: "site_direction_profiles",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "UX_organization_direction_one_active_per_organization",
                schema: "organization",
                table: "organization_direction_profiles",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_site_direction_one_active_per_organization",
                schema: "organization",
                table: "site_direction_profiles");

            migrationBuilder.DropIndex(
                name: "UX_organization_direction_one_active_per_organization",
                schema: "organization",
                table: "organization_direction_profiles");
        }
    }
}
