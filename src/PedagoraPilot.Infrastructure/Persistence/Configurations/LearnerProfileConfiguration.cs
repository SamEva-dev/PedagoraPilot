using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
{
    public void Configure(EntityTypeBuilder<LearnerProfile> b)
    {
        b.ToTable("learner_profiles", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new LearnerProfileId(x)).ValueGeneratedNever();
        b.Property(x => x.PersonId).HasConversion(x => x.Value, x => new PersonId(x)).IsRequired();
        b.Property(x => x.AuthGateUserId);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.ExternalKey).HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.PersonId).IsUnique();
        b.HasIndex(x => x.AuthGateUserId).IsUnique();
        b.HasIndex(x => x.ExternalKey).IsUnique();
    }
}
