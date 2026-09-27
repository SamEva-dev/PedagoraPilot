using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;

public sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> b)
    {
        b.ToTable("organization_members", SchemaNames.Organization);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Ignore(x => x.DisplayName);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new OrganizationMemberId(x)).ValueGeneratedNever();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.AuthGateUserId).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.AuthGateUserId }).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.Email });
    }
}

internal static class MemberRoleProfileConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> b, string tableName) where TEntity : class
    {
        b.ToTable(tableName, SchemaNames.Organization);
        b.HasKey("Id");
        b.Property<Guid>("Id").ValueGeneratedNever();
        b.Property<OrganizationMemberId>("MemberId").HasConversion(x => x.Value, x => new OrganizationMemberId(x)).IsRequired();
        b.Property<Guid>("OrganizationId").IsRequired();
        b.Property<bool>("IsActive").IsRequired();
        b.HasIndex("MemberId").IsUnique();
        b.HasIndex("OrganizationId", "IsActive");
    }
}

public sealed class OrganizationAdministratorProfileConfiguration : IEntityTypeConfiguration<OrganizationAdministratorProfile> { public void Configure(EntityTypeBuilder<OrganizationAdministratorProfile> b) => MemberRoleProfileConfiguration.Configure(b, "organization_administrator_profiles"); }
public sealed class OrganizationDirectionProfileConfiguration : IEntityTypeConfiguration<OrganizationDirectionProfile> { public void Configure(EntityTypeBuilder<OrganizationDirectionProfile> b) => MemberRoleProfileConfiguration.Configure(b, "organization_direction_profiles"); }
public sealed class SiteDirectionProfileConfiguration : IEntityTypeConfiguration<SiteDirectionProfile> { public void Configure(EntityTypeBuilder<SiteDirectionProfile> b) => MemberRoleProfileConfiguration.Configure(b, "site_direction_profiles"); }
public sealed class PedagogicalManagerProfileConfiguration : IEntityTypeConfiguration<PedagogicalManagerProfile> { public void Configure(EntityTypeBuilder<PedagogicalManagerProfile> b) => MemberRoleProfileConfiguration.Configure(b, "pedagogical_manager_profiles"); }
public sealed class SecretariatProfileConfiguration : IEntityTypeConfiguration<SecretariatProfile> { public void Configure(EntityTypeBuilder<SecretariatProfile> b) => MemberRoleProfileConfiguration.Configure(b, "secretariat_profiles"); }
public sealed class TrainerProfileConfiguration : IEntityTypeConfiguration<TrainerProfile> { public void Configure(EntityTypeBuilder<TrainerProfile> b) => MemberRoleProfileConfiguration.Configure(b, "trainer_profiles"); }
public sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile> { public void Configure(EntityTypeBuilder<StudentProfile> b) => MemberRoleProfileConfiguration.Configure(b, "student_profiles"); }
public sealed class JuryProfileConfiguration : IEntityTypeConfiguration<JuryProfile> { public void Configure(EntityTypeBuilder<JuryProfile> b) => MemberRoleProfileConfiguration.Configure(b, "jury_profiles"); }
public sealed class ReadOnlyProfileConfiguration : IEntityTypeConfiguration<ReadOnlyProfile> { public void Configure(EntityTypeBuilder<ReadOnlyProfile> b) => MemberRoleProfileConfiguration.Configure(b, "read_only_profiles"); }
