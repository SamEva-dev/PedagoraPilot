using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddCohortTrainerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleTrainerAuthGateUserId",
                schema: "catalog",
                table: "training_programs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleTrainerDisplayName",
                schema: "catalog",
                table: "training_programs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cohort_trainer_assignments",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthGateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cohort_trainer_assignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cohort_trainer_assignments_OrganizationId_AuthGateUserId_Is~",
                schema: "training",
                table: "cohort_trainer_assignments",
                columns: new[] { "OrganizationId", "AuthGateUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_cohort_trainer_assignments_OrganizationId_CohortId_AuthGate~",
                schema: "training",
                table: "cohort_trainer_assignments",
                columns: new[] { "OrganizationId", "CohortId", "AuthGateUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cohort_trainer_assignments_OrganizationId_CohortId_IsActive",
                schema: "training",
                table: "cohort_trainer_assignments",
                columns: new[] { "OrganizationId", "CohortId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cohort_trainer_assignments",
                schema: "training");

            migrationBuilder.DropColumn(
                name: "ResponsibleTrainerAuthGateUserId",
                schema: "catalog",
                table: "training_programs");

            migrationBuilder.DropColumn(
                name: "ResponsibleTrainerDisplayName",
                schema: "catalog",
                table: "training_programs");
        }
    }
}
