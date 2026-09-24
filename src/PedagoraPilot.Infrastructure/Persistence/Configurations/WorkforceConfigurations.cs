using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workforce;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class RemoteWorkRequestConfiguration : IEntityTypeConfiguration<RemoteWorkRequest>
{
    public void Configure(EntityTypeBuilder<RemoteWorkRequest> b)
    {
        b.ToTable("remote_work_requests", SchemaNames.Workforce);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new RemoteWorkRequestId(x)).ValueGeneratedNever();
        b.Property(x => x.UserDisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.UserEmail).HasMaxLength(250);
        b.Property(x => x.Period).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.Property(x => x.ApproverDisplayName).HasMaxLength(200);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.SiteId, x.Date });
        b.HasIndex(x => new { x.AuthGateUserId, x.Date });
        b.HasMany(x => x.Activities).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class RemoteWorkActivityConfiguration : IEntityTypeConfiguration<RemoteWorkActivity>
{
    public void Configure(EntityTypeBuilder<RemoteWorkActivity> b)
    {
        b.ToTable("remote_work_activities", SchemaNames.Workforce);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new RemoteWorkActivityId(x)).ValueGeneratedNever();
        b.Property(x => x.RequestId).HasConversion(x => x.Value, x => new RemoteWorkRequestId(x));
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Label).HasMaxLength(300).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.HasIndex(x => new { x.RequestId, x.Code }).IsUnique();
    }
}
