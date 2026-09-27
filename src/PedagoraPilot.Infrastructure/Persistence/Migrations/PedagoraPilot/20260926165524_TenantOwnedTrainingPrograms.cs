using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class TenantOwnedTrainingPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_training_programs_code",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.DropIndex(
                name: "IX_training_programs_external_key",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                schema: "catalog",
                table: "training_programs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_code",
                schema: "catalog",
                table: "training_programs",
                column: "code",
                unique: true,
                filter: "\"organization_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_external_key",
                schema: "catalog",
                table: "training_programs",
                column: "external_key",
                unique: true,
                filter: "\"organization_id\" IS NULL AND \"external_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_organization_id_code",
                schema: "catalog",
                table: "training_programs",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_organization_id_external_key",
                schema: "catalog",
                table: "training_programs",
                columns: new[] { "organization_id", "external_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_training_programs_code",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.DropIndex(
                name: "IX_training_programs_external_key",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.DropIndex(
                name: "IX_training_programs_organization_id_code",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.DropIndex(
                name: "IX_training_programs_organization_id_external_key",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.DropColumn(
                name: "organization_id",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_code",
                schema: "catalog",
                table: "training_programs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_programs_external_key",
                schema: "catalog",
                table: "training_programs",
                column: "external_key",
                unique: true);
        }
    }
}
