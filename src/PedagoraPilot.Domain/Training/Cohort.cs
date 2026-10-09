using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Events;

namespace PedagoraPilot.Domain.Training;
public sealed class Cohort : AggregateRoot<CohortId>
{
    private Cohort()
    {
    }

    private Cohort(CohortId id, Guid organizationId, Guid siteId, Guid programOfferingId, Guid referentialVersionId, string code, string name, DateOnly startDate, DateOnly endDate, int capacity, Guid? pedagogicalManagerAuthGateUserId, string? pedagogicalManagerDisplayName, string? externalKey, CohortStatus initialStatus) : base(id)
    {
        OrganizationId = organizationId;
        SiteId = siteId;
        ProgramOfferingId = programOfferingId;
        ReferentialVersionId = referentialVersionId;
        Code = NormalizeCode(code);
        Name = NormalizeRequired(name, "COHORT_NAME_REQUIRED");
        ValidatePeriod(startDate, endDate);
        ValidateCapacity(capacity);
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
        PedagogicalManagerAuthGateUserId = pedagogicalManagerAuthGateUserId;
        PedagogicalManagerDisplayName = NormalizeOptional(pedagogicalManagerDisplayName);
        ExternalKey = NormalizeOptional(externalKey);
        if (initialStatus is not (CohortStatus.Draft or CohortStatus.Planned or CohortStatus.Active))
            throw new DomainException("COHORT_STATUS_INVALID");
        Status = initialStatus;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid ProgramOfferingId { get; private set; }
    public Guid ReferentialVersionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int Capacity { get; private set; }
    public Guid? PedagogicalManagerAuthGateUserId { get; private set; }
    public string? PedagogicalManagerDisplayName { get; private set; }
    public CohortStatus Status { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Cohort Create(Guid organizationId, Guid siteId, Guid programOfferingId, Guid referentialVersionId, string code, string name, DateOnly startDate, DateOnly endDate, int capacity, Guid? pedagogicalManagerAuthGateUserId, string? pedagogicalManagerDisplayName, string? externalKey = null, CohortStatus initialStatus = CohortStatus.Planned)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("COHORT_ORGANIZATION_REQUIRED");
        if (siteId == Guid.Empty)
            throw new DomainException("COHORT_SITE_REQUIRED");
        if (programOfferingId == Guid.Empty)
            throw new DomainException("COHORT_PROGRAM_OFFERING_REQUIRED");
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("COHORT_REFERENTIAL_VERSION_REQUIRED");
        var cohort = new Cohort(CohortId.New(), organizationId, siteId, programOfferingId, referentialVersionId, code, name, startDate, endDate, capacity, pedagogicalManagerAuthGateUserId, pedagogicalManagerDisplayName, externalKey, initialStatus);
        cohort.RaiseDomainEvent(new CohortCreatedDomainEvent(cohort.Id, organizationId, programOfferingId));
        return cohort;
    }

    public void Update(string name, DateOnly startDate, DateOnly endDate, int capacity, CohortStatus status, Guid? pedagogicalManagerAuthGateUserId = null, string? pedagogicalManagerDisplayName = null)
    {
        if (Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new DomainException("COHORT_CLOSED");
        ValidatePeriod(startDate, endDate);
        ValidateCapacity(capacity);
        Name = NormalizeRequired(name, "COHORT_NAME_REQUIRED");
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
        if (pedagogicalManagerAuthGateUserId.HasValue) PedagogicalManagerAuthGateUserId = pedagogicalManagerAuthGateUserId;
        if (!string.IsNullOrWhiteSpace(pedagogicalManagerDisplayName)) PedagogicalManagerDisplayName = NormalizeOptional(pedagogicalManagerDisplayName);
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CohortUpdatedDomainEvent(Id, OrganizationId));
    }

    /// <summary>Explicit management transfer; unlike TryAssignPedagogicalManager, this is allowed to replace an existing owner.</summary>
    public void ReassignPedagogicalManager(Guid authGateUserId, string displayName)
    {
        if (Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new DomainException("COHORT_CLOSED");
        if (authGateUserId == Guid.Empty || string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("COHORT_PEDAGOGICAL_MANAGER_REQUIRED");
        var normalizedName = NormalizeOptional(displayName);
        if (PedagogicalManagerAuthGateUserId == authGateUserId && PedagogicalManagerDisplayName == normalizedName)
            return;
        PedagogicalManagerAuthGateUserId = authGateUserId;
        PedagogicalManagerDisplayName = normalizedName;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CohortUpdatedDomainEvent(Id, OrganizationId));
    }

    public bool TryAssignPedagogicalManager(Guid authGateUserId, string displayName)
    {
        if (authGateUserId == Guid.Empty)
            throw new DomainException("COHORT_PEDAGOGICAL_MANAGER_REQUIRED");

        if (PedagogicalManagerAuthGateUserId.HasValue
            && PedagogicalManagerAuthGateUserId.Value != authGateUserId)
            return false;

        var normalizedName = NormalizeOptional(displayName);
        if (PedagogicalManagerAuthGateUserId == authGateUserId
            && string.Equals(PedagogicalManagerDisplayName, normalizedName, StringComparison.Ordinal))
            return true;

        PedagogicalManagerAuthGateUserId = authGateUserId;
        PedagogicalManagerDisplayName = normalizedName;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CohortUpdatedDomainEvent(Id, OrganizationId));
        return true;
    }

    public void MarkDeleted() => RaiseDomainEvent(new CohortDeletedDomainEvent(Id, OrganizationId));

    private static string NormalizeCode(string value)
    {
        var code = NormalizeRequired(value, "COHORT_CODE_REQUIRED").ToUpperInvariant();
        if (code.Length > 64)
            throw new DomainException("COHORT_CODE_INVALID");
        return code;
    }

    private static string NormalizeRequired(string value, string errorKey)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(errorKey);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidatePeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new DomainException("COHORT_DATE_RANGE_INVALID");
    }

    private static void ValidateCapacity(int capacity)
    {
        if (capacity <= 0 || capacity > 500)
            throw new DomainException("COHORT_CAPACITY_INVALID");
    }
}
