using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddWorkplaceTutorAssignmentsLinkWorkplaceTutorInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tutor_assignments",
                schema: "workplace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkplacePeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AuthGateUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tutor_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tutor_assignments_periods_WorkplacePeriodId",
                        column: x => x.WorkplacePeriodId,
                        principalSchema: "workplace",
                        principalTable: "periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tutor_assignments_OrganizationId_InvitationId",
                schema: "workplace",
                table: "tutor_assignments",
                columns: new[] { "OrganizationId", "InvitationId" });

            migrationBuilder.CreateIndex(
                name: "IX_tutor_assignments_OrganizationId_WorkplacePeriodId_AuthGate~",
                schema: "workplace",
                table: "tutor_assignments",
                columns: new[] { "OrganizationId", "WorkplacePeriodId", "AuthGateUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_tutor_assignments_OrganizationId_WorkplacePeriodId_InvitedE~",
                schema: "workplace",
                table: "tutor_assignments",
                columns: new[] { "OrganizationId", "WorkplacePeriodId", "InvitedEmail" },
                unique: true,
                filter: "\"Status\" <> 'Revoked'");

            migrationBuilder.CreateIndex(
                name: "IX_tutor_assignments_WorkplacePeriodId",
                schema: "workplace",
                table: "tutor_assignments",
                column: "WorkplacePeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tutor_assignments",
                schema: "workplace");
        }
    }
}
