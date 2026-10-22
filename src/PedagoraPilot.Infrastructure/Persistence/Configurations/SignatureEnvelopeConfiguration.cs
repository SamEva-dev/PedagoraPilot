using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;

public sealed class SignatureEnvelopeConfiguration : IEntityTypeConfiguration<SignatureEnvelope>
{
    public void Configure(EntityTypeBuilder<SignatureEnvelope> b)
    {
        b.ToTable("signature_envelopes", SchemaNames.Signature);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new SignatureEnvelopeId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x)).IsRequired();
        b.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(48).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.RequestedByUserId).HasMaxLength(64).IsRequired();
        b.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
        b.Property(x => x.SnapshotJson).HasColumnType("text").IsRequired();
        b.Property(x => x.EvidenceReference).HasMaxLength(500);
        b.Property(x => x.ResponseComment).HasMaxLength(2000);
        b.Property(x => x.ArchivePdfStorageKey).HasMaxLength(1024);
        b.Property(x => x.ArchivePdfSha256).HasMaxLength(64);
        b.Property(x => x.ArchiveManifestStorageKey).HasMaxLength(1024);
        b.Property(x => x.ArchiveManifestSha256).HasMaxLength(64);
        b.Property(x => x.ArchivedAtUtc).HasColumnType("timestamp with time zone");
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.ReminderCount).HasDefaultValue(0);
        b.HasIndex(x => new { x.OrganizationId, x.SourceType, x.SourceId, x.SourceVersion, x.EnrollmentId }).IsUnique().HasDatabaseName("ux_signature_source_revision");
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.Status }).HasDatabaseName("ix_signature_cohort_status");
        b.HasIndex(x => new { x.OrganizationId, x.SignerAuthGateUserId, x.Status }).HasDatabaseName("ix_signature_signer_status");
        b.HasIndex(x => new { x.OrganizationId, x.RequestedByUserId, x.CreatedAtUtc }).HasDatabaseName("ix_signature_requester_created");
    }
}
