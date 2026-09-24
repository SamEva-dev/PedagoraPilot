using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class AttendanceSheetConfiguration : IEntityTypeConfiguration<AttendanceSheet>
{
    public void Configure(EntityTypeBuilder<AttendanceSheet> b)
    {
        b.ToTable("attendance_sheets", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new AttendanceSheetId(x)).ValueGeneratedNever();
        b.Property(x => x.SessionId).HasConversion(x => x.Value, x => new TrainingSessionId(x)).IsRequired();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x)).IsRequired();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.ExpectedMinutes).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.SessionId).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId });
        b.HasMany(x => x.Entries).WithOne().HasForeignKey("attendance_sheet_id").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Entries).HasField("_entries").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class AttendanceEntryConfiguration : IEntityTypeConfiguration<AttendanceEntry>
{
    public void Configure(EntityTypeBuilder<AttendanceEntry> b)
    {
        b.ToTable("attendance_entries", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new AttendanceEntryId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x)).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.ArrivalAtUtc);
        b.Property(x => x.DepartureAtUtc);
        b.Property(x => x.ExpectedMinutes).IsRequired();
        b.Property(x => x.PresentMinutes).IsRequired();
        b.Property(x => x.MissedMinutes).IsRequired();
        b.Property(x => x.CatchupMinutes).IsRequired();
        b.Property(x => x.AddToCatchup).IsRequired();
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.HasIndex(x => x.EnrollmentId);
        b.Property<AttendanceSheetId>("attendance_sheet_id").HasConversion(x => x.Value, x => new AttendanceSheetId(x));
        b.HasIndex("attendance_sheet_id", nameof(AttendanceEntry.EnrollmentId)).IsUnique();
    }
}
