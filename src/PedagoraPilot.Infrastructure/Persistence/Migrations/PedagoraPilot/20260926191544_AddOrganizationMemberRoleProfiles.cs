using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddOrganizationMemberRoleProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jury_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jury_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organization_administrator_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_administrator_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organization_direction_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_direction_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organization_members",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthGateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pedagogical_manager_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedagogical_manager_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "read_only_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_read_only_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "secretariat_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secretariat_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_direction_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_direction_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "student_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trainer_profiles",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_jury_profiles_MemberId",
                schema: "organization",
                table: "jury_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jury_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "jury_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_administrator_profiles_MemberId",
                schema: "organization",
                table: "organization_administrator_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_administrator_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "organization_administrator_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_direction_profiles_MemberId",
                schema: "organization",
                table: "organization_direction_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_direction_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "organization_direction_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_OrganizationId_AuthGateUserId",
                schema: "organization",
                table: "organization_members",
                columns: new[] { "OrganizationId", "AuthGateUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_OrganizationId_Email",
                schema: "organization",
                table: "organization_members",
                columns: new[] { "OrganizationId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_pedagogical_manager_profiles_MemberId",
                schema: "organization",
                table: "pedagogical_manager_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedagogical_manager_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "pedagogical_manager_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_read_only_profiles_MemberId",
                schema: "organization",
                table: "read_only_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_read_only_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "read_only_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_secretariat_profiles_MemberId",
                schema: "organization",
                table: "secretariat_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_secretariat_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "secretariat_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_site_direction_profiles_MemberId",
                schema: "organization",
                table: "site_direction_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_direction_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "site_direction_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_MemberId",
                schema: "organization",
                table: "student_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "student_profiles",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_profiles_MemberId",
                schema: "organization",
                table: "trainer_profiles",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_profiles_OrganizationId_IsActive",
                schema: "organization",
                table: "trainer_profiles",
                columns: new[] { "OrganizationId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jury_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "organization_administrator_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "organization_direction_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "organization_members",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "pedagogical_manager_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "read_only_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "secretariat_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "site_direction_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "student_profiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "trainer_profiles",
                schema: "organization");
        }
    }
}
