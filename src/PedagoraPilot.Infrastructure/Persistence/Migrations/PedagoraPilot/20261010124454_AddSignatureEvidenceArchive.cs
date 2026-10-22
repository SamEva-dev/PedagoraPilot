using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedagoraPilot.Infrastructure.Persistence.Migrations.PedagoraPilot
{
    /// <inheritdoc />
    public partial class AddSignatureEvidenceArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveManifestSha256",
                schema: "signature",
                table: "signature_envelopes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveManifestStorageKey",
                schema: "signature",
                table: "signature_envelopes",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivePdfSha256",
                schema: "signature",
                table: "signature_envelopes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivePdfStorageKey",
                schema: "signature",
                table: "signature_envelopes",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAtUtc",
                schema: "signature",
                table: "signature_envelopes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveManifestSha256",
                schema: "signature",
                table: "signature_envelopes");

            migrationBuilder.DropColumn(
                name: "ArchiveManifestStorageKey",
                schema: "signature",
                table: "signature_envelopes");

            migrationBuilder.DropColumn(
                name: "ArchivePdfSha256",
                schema: "signature",
                table: "signature_envelopes");

            migrationBuilder.DropColumn(
                name: "ArchivePdfStorageKey",
                schema: "signature",
                table: "signature_envelopes");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                schema: "signature",
                table: "signature_envelopes");
        }
    }
}
