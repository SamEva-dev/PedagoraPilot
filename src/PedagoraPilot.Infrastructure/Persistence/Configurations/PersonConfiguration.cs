using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("people", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Ignore(x => x.DisplayName);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new PersonId(x)).ValueGeneratedNever();
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.ExternalKey).HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.ExternalKey).IsUnique();
    }
}
