using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddOrganizationCommercialReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCustomerReference",
                schema: "organization",
                table: "organizations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAtUtc",
                schema: "organization",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                schema: "organization",
                table: "organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                schema: "organization",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAtUtc",
                schema: "organization",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationComment",
                schema: "organization",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                schema: "organization",
                table: "organizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedAtUtc",
                schema: "organization",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAtUtc",
                schema: "organization",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingCustomerReference",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAtUtc",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "TrialEndsAtUtc",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "VerificationComment",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedAtUtc",
                schema: "organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "VerifiedAtUtc",
                schema: "organization",
                table: "organizations");
        }
    }
}
