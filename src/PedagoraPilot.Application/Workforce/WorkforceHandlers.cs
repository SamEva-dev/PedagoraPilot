using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Workforce;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workforce;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Workforce;
internal static class WfMap
{
    public static RemoteWorkRequestDto Request(RemoteWorkRequest x, IObjectMapper mapper)
    {
        var m = mapper.Map<RemoteWorkRequest, RemoteWorkRequestReadModel>(x);
        return new(m.Id.Value, m.OrganizationId, m.SiteId, m.AuthGateUserId, m.UserDisplayName, m.UserEmail, m.Date, m.Period.ToString(), m.StartTime, m.EndTime, m.Status.ToString(), m.Comment, m.ApproverUserId, m.ApproverDisplayName, m.DecidedAtUtc, x.Activities.Select(a =>
        {
            var q = mapper.Map<RemoteWorkActivity, RemoteWorkActivityReadModel>(a);
            return new RemoteWorkActivityDto(q.Id.Value, q.Code, q.Label, q.Status.ToString());
        }).ToArray());
    }
}

public sealed class GetRemoteWorkRequestsQueryHandler(IRemoteWorkRequestRepository repo, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<GetRemoteWorkRequestsQuery, IReadOnlyCollection<RemoteWorkRequestDto>>
{
    public async Task<IReadOnlyCollection<RemoteWorkRequestDto>> Handle(GetRemoteWorkRequestsQuery r, CancellationToken ct)
    {
        var q = repo.Query(false).Include(x => x.Activities).AsQueryable();
        var organizationId = TenantScope.Organization(current);
        if (!current.UserId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        // A client-supplied mineOnly=false must never expose coworkers to a View-only user.
        if (!organizationId.HasValue && !r.SiteId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        if (organizationId.HasValue)
            q = q.Where(x => x.OrganizationId == organizationId.Value);
        if (r.SiteId.HasValue)
        {
            ContextualScope.EnsureCanViewSite(current, r.SiteId.Value);
            q = q.Where(x => x.SiteId == r.SiteId.Value);
        }
        if (r.MineOnly || !current.HasPermission(PedagoraPilotPermissionCodes.RemoteWork.Manage))
            q = q.Where(x => x.AuthGateUserId == current.UserId.Value);
        var rows = await q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        if (current.HasContextualScopeRestrictions)
            rows = rows.Where(x => current.CanViewSite(x.SiteId)).ToList();
        return rows.Select(x => WfMap.Request(x, mapper)).ToArray();
    }
}

public sealed class CreateRemoteWorkCommandHandler(IRemoteWorkRequestRepository repo, ITrainingSiteRepository sites, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<CreateRemoteWorkCommand, RemoteWorkRequestDto>
{
    public async Task<RemoteWorkRequestDto> Handle(CreateRemoteWorkCommand r, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue || !current.UserId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        if (!await sites.Query(false).AnyAsync(x => x.Id == r.SiteId && x.OrganizationId == current.OrganizationId.Value, ct))
            throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        ContextualScope.EnsureCanViewSite(current, r.SiteId);
        if (!Enum.TryParse<RemoteWorkPeriod>(Normalize(r.Period), true, out var period))
            throw new ValidationApplicationException(ErrorKeys.RemoteWorkPeriodInvalid);
        if (await repo.HasOverlappingRequestAsync(current.UserId.Value, r.Date, null, ct))
            throw new ConflictApplicationException(ErrorKeys.RemoteWorkOverlap);
        var displayName = current.DisplayName ?? current.Email ?? current.UserId.Value.ToString();
        var x = RemoteWorkRequest.Create(current.OrganizationId.Value, r.SiteId, current.UserId.Value, displayName, current.Email, r.Date, period, r.StartTime, r.EndTime, r.Comment, (r.Activities ?? []).Select(a => (a.Code, a.Label)));
        await repo.AddAsync(x, ct);
        return WfMap.Request(x, mapper);
    }

    private static string Normalize(string value) => value.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
}

public sealed class DecideRemoteWorkCommandHandler(IRemoteWorkRequestRepository repo, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<DecideRemoteWorkCommand, RemoteWorkRequestDto>
{
    public async Task<RemoteWorkRequestDto> Handle(DecideRemoteWorkCommand r, CancellationToken ct)
    {
        var x = await repo.Query(true).Include(a => a.Activities).SingleOrDefaultAsync(a => a.Id == r.RequestId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.RemoteWorkNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        ContextualScope.EnsureCanManageSite(current, x.SiteId);
        if (!current.UserId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        var name = current.DisplayName ?? current.Email ?? "Pedagora Pilot";
        if (r.Approved)
            x.Approve(current.UserId.Value, name);
        else
            x.Reject(current.UserId.Value, name);
        return WfMap.Request(x, mapper);
    }
}

public sealed class UpdateRemoteWorkActivityCommandHandler(IRemoteWorkRequestRepository repo, ICurrentUser current, IObjectMapper mapper) : IRequestHandler<UpdateRemoteWorkActivityCommand, RemoteWorkRequestDto>
{
    public async Task<RemoteWorkRequestDto> Handle(UpdateRemoteWorkActivityCommand r, CancellationToken ct)
    {
        var x = await repo.Query(true).Include(a => a.Activities).SingleOrDefaultAsync(a => a.Id == r.RequestId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.RemoteWorkNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        ContextualScope.EnsureCanViewSite(current, x.SiteId);
        if (!current.UserId.HasValue || x.AuthGateUserId != current.UserId.Value)
            throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        if (!Enum.TryParse<RemoteWorkActivityStatus>(r.Status, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.RemoteWorkActivityStatusInvalid);
        x.SetActivity(r.ActivityId, status);
        return WfMap.Request(x, mapper);
    }
}

public sealed class GetRemoteWorkPolicyQueryHandler(IOrganizationRepository organizations, ICurrentUser current)
    : IRequestHandler<GetRemoteWorkPolicyQuery, RemoteWorkPolicyDto>
{
    public async Task<RemoteWorkPolicyDto> Handle(GetRemoteWorkPolicyQuery _, CancellationToken ct)
    {
        var organizationId = TenantScope.Organization(current)
            ?? throw new ForbiddenApplicationException(ErrorKeys.RemoteWorkForbidden);
        var organization = await organizations.GetByIdAsync(organizationId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
        return new RemoteWorkPolicyDto(
            organization.RemoteWorkEnabled,
            organization.RemoteWorkApprovalRequired,
            Math.Max(0, organization.RemoteWorkMaxDaysPerWeek),
            organization.RemoteWorkHalfDayAllowed,
            organization.RemoteWorkEndOfDayReport);
    }
}
