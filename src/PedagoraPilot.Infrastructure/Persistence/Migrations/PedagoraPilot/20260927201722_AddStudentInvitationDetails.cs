using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddStudentInvitationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                schema: "organization",
                table: "access_invitations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EnrolledOn",
                schema: "organization",
                table: "access_invitations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "organization",
                table: "access_invitations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDate",
                schema: "organization",
                table: "access_invitations");

            migrationBuilder.DropColumn(
                name: "EnrolledOn",
                schema: "organization",
                table: "access_invitations");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "organization",
                table: "access_invitations");
        }
    }
}
