using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Organizations.Events;
using PedagoraPilot.Domain.Organizations.ValueObjects;

namespace PedagoraPilot.Domain.Organizations;

public sealed class Organization : AggregateRoot, IAuditableEntity
{
    private Organization()
    {
    }

    private Organization(Guid id, Guid ownerUserId, OrganizationCode code, string legalName, string countryCode, string ownerEmail, string? ownerPhone) : base(id)
    {
        OwnerUserId = ownerUserId;
        Code = code;
        LegalName = legalName;
        ShortName = legalName;
        CountryCode = countryCode;
        OwnerEmail = ownerEmail;
        OwnerPhone = ownerPhone;
        PrimaryColor = "#1456A0";
        SecondaryColor = "#F59E0B";
        LogoLabel = legalName;
        Language = "fr";
        Timezone = "Europe/Paris";
        DateFormat = "DD/MM/YYYY";
        EnabledModulesCsv = DefaultEnabledModules;
        AbsenceAlerts = true;
        CertificationAlerts = true;
        WeeklyDigest = true;
        StrictAudit = true;
        RemoteWorkEnabled = true;
        RemoteWorkApprovalRequired = true;
        RemoteWorkMaxDaysPerWeek = 2;
        RemoteWorkHalfDayAllowed = true;
        RemoteWorkEndOfDayReport = true;
        Status = OrganizationStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        var @event = new OrganizationProvisionedDomainEvent(Id, OwnerUserId, Code.Value, LegalName);
        RaiseDomainEvent(@event);
    }

    private const string DefaultEnabledModules = "planning,attendance,sessions,driving,plateau,sheets,skills,internships,documents,certification,statistics";

    public Guid OwnerUserId { get; private set; }
    public OrganizationCode Code { get; private set; } = null!;
    public string LegalName { get; private set; } = string.Empty;
    public string ShortName { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "FR";
    public string OwnerEmail { get; private set; } = string.Empty;
    public string? OwnerPhone { get; private set; }
    public string Siret { get; private set; } = string.Empty;
    public string TrainingDeclarationNumber { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Website { get; private set; } = string.Empty;
    public string ManagerName { get; private set; } = string.Empty;
    public string PrimaryColor { get; private set; } = "#1456A0";
    public string SecondaryColor { get; private set; } = "#F59E0B";
    public string LoginTagline { get; private set; } = string.Empty;
    public string LogoLabel { get; private set; } = string.Empty;
    public bool WhiteLabel { get; private set; }
    public bool AllowSiteOverrides { get; private set; } = true;
    public string EnabledModulesCsv { get; private set; } = DefaultEnabledModules;
    public bool AbsenceAlerts { get; private set; } = true;
    public bool CertificationAlerts { get; private set; } = true;
    public bool WeeklyDigest { get; private set; } = true;
    public bool AutoArchive { get; private set; }
    public bool StrictAudit { get; private set; } = true;
    public string Language { get; private set; } = "fr";
    public string Timezone { get; private set; } = "Europe/Paris";
    public string DateFormat { get; private set; } = "DD/MM/YYYY";
    public string AcademicYear { get; private set; } = string.Empty;
    public bool RemoteWorkEnabled { get; private set; } = true;
    public bool RemoteWorkApprovalRequired { get; private set; } = true;
    public int RemoteWorkMaxDaysPerWeek { get; private set; } = 2;
    public bool RemoteWorkHalfDayAllowed { get; private set; } = true;
    public bool RemoteWorkEndOfDayReport { get; private set; } = true;
    public OrganizationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    DateTimeOffset IAuditableEntity.CreatedAtUtc => new(CreatedAtUtc, TimeSpan.Zero);
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? LastModifiedAtUtc { get; private set; }
    public Guid? LastModifiedByUserId { get; private set; }

    public IReadOnlyCollection<string> EnabledModules => EnabledModulesCsv
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public void SetCreatedAudit(DateTimeOffset createdAtUtc, Guid? createdByUserId)
    {
        CreatedAtUtc = createdAtUtc.UtcDateTime;
        CreatedByUserId = createdByUserId;
    }

    public void SetModifiedAudit(DateTimeOffset modifiedAtUtc, Guid? modifiedByUserId)
    {
        LastModifiedAtUtc = modifiedAtUtc.ToUniversalTime();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAtUtc = modifiedAtUtc.UtcDateTime;
    }

    public bool IsLoginAllowed => Status is OrganizationStatus.Draft or OrganizationStatus.Active or OrganizationStatus.Restricted;

    public static Organization Provision(Guid ownerUserId, string code, string legalName, string? countryCode, string ownerEmail, string? ownerPhone)
    {
        if (ownerUserId == Guid.Empty)
            throw new DomainException("ORGANIZATION_OWNER_REQUIRED");
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerEmail);
        return new Organization(Guid.NewGuid(), ownerUserId, OrganizationCode.Create(code), legalName.Trim(), NormalizeCountry(countryCode), ownerEmail.Trim().ToLowerInvariant(), string.IsNullOrWhiteSpace(ownerPhone) ? null : ownerPhone.Trim());
    }

    public void UpdateAdministration(
        string legalName,
        string shortName,
        string siret,
        string trainingDeclarationNumber,
        string address,
        string postalCode,
        string city,
        string countryCode,
        string email,
        string phone,
        string website,
        string managerName,
        string primaryColor,
        string secondaryColor,
        string loginTagline,
        string logoLabel,
        bool whiteLabel,
        bool allowSiteOverrides,
        IEnumerable<string> enabledModules,
        bool absenceAlerts,
        bool certificationAlerts,
        bool weeklyDigest,
        bool autoArchive,
        bool strictAudit,
        string language,
        string timezone,
        string dateFormat,
        string academicYear,
        bool remoteWorkEnabled,
        bool remoteWorkApprovalRequired,
        int remoteWorkMaxDaysPerWeek,
        bool remoteWorkHalfDayAllowed,
        bool remoteWorkEndOfDayReport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        var normalizedModules = (enabledModules ?? Array.Empty<string>())
            .Select(x => x?.Trim() ?? string.Empty)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (remoteWorkMaxDaysPerWeek is < 0 or > 7)
            throw new DomainException("REMOTE_WORK_MAX_DAYS_INVALID");

        LegalName = legalName.Trim();
        ShortName = string.IsNullOrWhiteSpace(shortName) ? LegalName : shortName.Trim();
        Siret = NormalizeText(siret);
        TrainingDeclarationNumber = NormalizeText(trainingDeclarationNumber);
        Address = NormalizeText(address);
        PostalCode = NormalizeText(postalCode);
        City = NormalizeText(city);
        CountryCode = NormalizeCountry(countryCode);
        OwnerEmail = NormalizeEmail(email);
        OwnerPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Website = NormalizeText(website);
        ManagerName = NormalizeText(managerName);
        PrimaryColor = NormalizeColor(primaryColor, "#1456A0");
        SecondaryColor = NormalizeColor(secondaryColor, "#F59E0B");
        LoginTagline = NormalizeText(loginTagline);
        LogoLabel = string.IsNullOrWhiteSpace(logoLabel) ? ShortName : logoLabel.Trim();
        WhiteLabel = whiteLabel;
        AllowSiteOverrides = allowSiteOverrides;
        EnabledModulesCsv = string.Join(',', normalizedModules);
        AbsenceAlerts = absenceAlerts;
        CertificationAlerts = certificationAlerts;
        WeeklyDigest = weeklyDigest;
        AutoArchive = autoArchive;
        StrictAudit = strictAudit;
        Language = string.IsNullOrWhiteSpace(language) ? "fr" : language.Trim().ToLowerInvariant();
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "Europe/Paris" : timezone.Trim();
        DateFormat = string.IsNullOrWhiteSpace(dateFormat) ? "DD/MM/YYYY" : dateFormat.Trim();
        AcademicYear = NormalizeText(academicYear);
        RemoteWorkEnabled = remoteWorkEnabled;
        RemoteWorkApprovalRequired = remoteWorkApprovalRequired;
        RemoteWorkMaxDaysPerWeek = remoteWorkMaxDaysPerWeek;
        RemoteWorkHalfDayAllowed = remoteWorkHalfDayAllowed;
        RemoteWorkEndOfDayReport = remoteWorkEndOfDayReport;
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;

        var @event = new OrganizationAdministrationUpdatedDomainEvent(Id);
        RaiseDomainEvent(@event);
    }

    private static string NormalizeCountry(string? value)
    {
        var country = string.IsNullOrWhiteSpace(value) ? "FR" : value.Trim().ToUpperInvariant();
        if (country.Length != 2)
            throw new DomainException("ORGANIZATION_COUNTRY_CODE_INVALID");
        return country;
    }

    private static string NormalizeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeColor(string? value, string fallback)
    {
        var color = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return color.Length == 7 && color[0] == '#' ? color.ToUpperInvariant() : fallback;
    }
}
