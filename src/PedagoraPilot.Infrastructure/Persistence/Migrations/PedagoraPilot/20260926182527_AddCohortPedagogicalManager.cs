using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddCohortPedagogicalManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PedagogicalManagerAuthGateUserId",
                schema: "training",
                table: "cohorts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PedagogicalManagerDisplayName",
                schema: "training",
                table: "cohorts",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PedagogicalManagerAuthGateUserId",
                schema: "training",
                table: "cohorts");

            migrationBuilder.DropColumn(
                name: "PedagogicalManagerDisplayName",
                schema: "training",
                table: "cohorts");
        }
    }
}
