using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Administration;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Organizations.Administration;

public sealed class GetOrganizationAdministrationQueryHandler(
    IOrganizationRepository organizations,
    ICurrentUser current)
    : IRequestHandler<GetOrganizationAdministrationQuery, OrganizationAdministrationDto>
{
    public async Task<OrganizationAdministrationDto> Handle(GetOrganizationAdministrationQuery request, CancellationToken ct)
    {
        TenantScope.Ensure(current, request.OrganizationId);
        if (current.HasContextualScopeRestrictions)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var organization = await organizations.GetByIdAsync(request.OrganizationId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        return Map(organization);
    }

    internal static OrganizationAdministrationDto Map(Organization x) => new(
        x.Id,
        x.Code.Value,
        x.LegalName ?? string.Empty,
        x.ShortName ?? string.Empty,
        x.Siret ?? string.Empty,
        x.TrainingDeclarationNumber ?? string.Empty,
        x.Address ?? string.Empty,
        x.PostalCode ?? string.Empty,
        x.City ?? string.Empty,
        x.CountryCode ?? string.Empty,
        x.OwnerEmail ?? string.Empty,
        x.OwnerPhone ?? string.Empty,
        x.Website ?? string.Empty,
        x.ManagerName ?? string.Empty,
        x.PrimaryColor ?? string.Empty,
        x.SecondaryColor ?? string.Empty,
        x.LoginTagline ?? string.Empty,
        x.LogoLabel ?? string.Empty,
        x.WhiteLabel,
        x.AllowSiteOverrides,
        x.EnabledModules,
        x.AbsenceAlerts,
        x.CertificationAlerts,
        x.WeeklyDigest,
        x.AutoArchive,
        x.StrictAudit,
        x.Language ?? string.Empty,
        x.Timezone ?? string.Empty,
        x.DateFormat ?? string.Empty,
        x.AcademicYear ?? string.Empty,
        x.RemoteWorkEnabled,
        x.RemoteWorkApprovalRequired,
        x.RemoteWorkMaxDaysPerWeek,
        x.RemoteWorkHalfDayAllowed,
        x.RemoteWorkEndOfDayReport);
}

public sealed class UpdateOrganizationAdministrationCommandHandler(
    IOrganizationRepository organizations,
    ICurrentUser current)
    : IRequestHandler<UpdateOrganizationAdministrationCommand, OrganizationAdministrationDto>
{
    public async Task<OrganizationAdministrationDto> Handle(UpdateOrganizationAdministrationCommand command, CancellationToken ct)
    {
        TenantScope.Ensure(current, command.OrganizationId);
        if (current.HasContextualScopeRestrictions)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var organization = await organizations.GetByIdAsync(command.OrganizationId, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        var r = command.Request;
        organization.UpdateAdministration(
            r.LegalName, r.ShortName, r.Siret, r.TrainingDeclarationNumber, r.Address, r.PostalCode,
            r.City, r.Country, r.Email, r.Phone, r.Website, r.ManagerName,
            r.PrimaryColor, r.SecondaryColor, r.LoginTagline, r.LogoLabel, r.WhiteLabel,
            r.AllowSiteOverrides, r.EnabledModules ?? Array.Empty<string>(), r.AbsenceAlerts,
            r.CertificationAlerts, r.WeeklyDigest, r.AutoArchive, r.StrictAudit,
            r.Language, r.Timezone, r.DateFormat, r.AcademicYear,
            r.RemoteWorkEnabled, r.RemoteWorkApprovalRequired, r.RemoteWorkMaxDaysPerWeek,
            r.RemoteWorkHalfDayAllowed, r.RemoteWorkEndOfDayReport);
        return GetOrganizationAdministrationQueryHandler.Map(organization);
    }
}
