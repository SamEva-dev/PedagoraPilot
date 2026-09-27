using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;

public sealed class AccessInvitationConfiguration : IEntityTypeConfiguration<AccessInvitation>
{
    public void Configure(EntityTypeBuilder<AccessInvitation> b)
    {
        b.ToTable("access_invitations", SchemaNames.Organization);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new AccessInvitationId(x)).ValueGeneratedNever();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        b.Property(x => x.RequestedRole).HasMaxLength(128).IsRequired();
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.InvitedByUserId).IsRequired();
        b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.Email, x.Status });
        b.HasIndex(x => new { x.OrganizationId, x.Status, x.ExpiresAtUtc });
    }
}
