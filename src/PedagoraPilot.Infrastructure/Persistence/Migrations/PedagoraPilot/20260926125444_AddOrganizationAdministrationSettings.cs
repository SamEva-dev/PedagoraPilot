using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddOrganizationAdministrationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Correction",
                schema: "learning",
                table: "pedagogical_topics",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Example",
                schema: "learning",
                table: "pedagogical_topics",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Objective",
                schema: "learning",
                table: "pedagogical_topics",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AbsenceAlerts",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                schema: "organization",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "organization",
                table: "organizations",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AllowSiteOverrides",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoArchive",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CertificationAlerts",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "organization",
                table: "organizations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DateFormat",
                schema: "organization",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnabledModulesCsv",
                schema: "organization",
                table: "organizations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "organization",
                table: "organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoginTagline",
                schema: "organization",
                table: "organizations",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoLabel",
                schema: "organization",
                table: "organizations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManagerName",
                schema: "organization",
                table: "organizations",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "organization",
                table: "organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                schema: "organization",
                table: "organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RemoteWorkApprovalRequired",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RemoteWorkEnabled",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RemoteWorkEndOfDayReport",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RemoteWorkHalfDayAllowed",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RemoteWorkMaxDaysPerWeek",
                schema: "organization",
                table: "organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                schema: "organization",
                table: "organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                schema: "organization",
                table: "organizations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Siret",
                schema: "organization",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "StrictAudit",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                schema: "organization",
                table: "organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrainingDeclarationNumber",
                schema: "organization",
                table: "organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                schema: "organization",
                table: "organizations",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "WeeklyDigest",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WhiteLabel",
                schema: "organization",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Correction",
                schema: "learning",
                table: "pedagogical_topics");

            migrationBuilder.DropColumn(
                name: "Example",
                schema: "learning",
                table: "pedagogical_topics");

            migrationBuilder.DropColumn(
                name: "Objective",
                schema: "learning",
                table: "pedagogical_topics");

            migrationBuilder.DropColumn(
                name: "AbsenceAlerts",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Address",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "AllowSiteOverrides",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "AutoArchive",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "CertificationAlerts",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "DateFormat",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "EnabledModulesCsv",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "LoginTagline",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "LogoLabel",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ManagerName",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "RemoteWorkApprovalRequired",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "RemoteWorkEnabled",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "RemoteWorkEndOfDayReport",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "RemoteWorkHalfDayAllowed",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "RemoteWorkMaxDaysPerWeek",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ShortName",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Siret",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "StrictAudit",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Timezone",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "TrainingDeclarationNumber",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Website",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "WeeklyDigest",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "WhiteLabel",
                schema: "organization",
                table: "organizations");
        }
    }
}
