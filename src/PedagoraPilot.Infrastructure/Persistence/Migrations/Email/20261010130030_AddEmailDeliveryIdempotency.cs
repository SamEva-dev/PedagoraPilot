using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.Email
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DispatchLeaseId",
                schema: "emailing",
                table: "EmailMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "emailing",
                table: "EmailMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadSha256",
                schema: "emailing",
                table: "EmailMessages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_DispatchLeaseId",
                schema: "emailing",
                table: "EmailMessages",
                column: "DispatchLeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_IdempotencyKey",
                schema: "emailing",
                table: "EmailMessages",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_DispatchLeaseId",
                schema: "emailing",
                table: "EmailMessages");

            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_IdempotencyKey",
                schema: "emailing",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "DispatchLeaseId",
                schema: "emailing",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "emailing",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "PayloadSha256",
                schema: "emailing",
                table: "EmailMessages");
        }
    }
}
