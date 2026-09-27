using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Provisioning;

namespace PedagoraPilot.Application.Access.Members;

public sealed record SyncOrganizationMemberCommand(
    Guid OrganizationId,
    Guid AuthGateUserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    bool Active) : ICommand<ProvisionOrganizationMemberResponse>;

public sealed class SyncOrganizationMemberCommandHandler(
    PedagoraPilot.Application.Abstractions.Persistence.IOrganizationRepository organizations,
    PedagoraPilot.Application.Abstractions.Persistence.IOrganizationMemberRepository members)
    : IRequestHandler<SyncOrganizationMemberCommand, ProvisionOrganizationMemberResponse>
{
    public async Task<ProvisionOrganizationMemberResponse> Handle(SyncOrganizationMemberCommand request, CancellationToken ct)
    {
        if (request.OrganizationId == Guid.Empty || request.AuthGateUserId == Guid.Empty)
            throw new ArgumentException("OrganizationId and AuthGateUserId are required.");
        _ = await organizations.GetByIdAsync(request.OrganizationId, false, ct)
            ?? throw new PedagoraPilot.Application.Common.Errors.NotFoundApplicationException("ORGANIZATION_NOT_FOUND");

        var member = await members.GetByAuthGateUserAsync(request.OrganizationId, request.AuthGateUserId, true, ct);
        if (member is null)
        {
            member = PedagoraPilot.Domain.Access.OrganizationMember.Create(
                request.OrganizationId, request.AuthGateUserId, request.Email, request.FirstName, request.LastName);
            if (!request.Active) member.SynchronizeIdentity(request.Email, request.FirstName, request.LastName, false);
            await members.AddAsync(member, ct);
        }
        else
        {
            member.SynchronizeIdentity(request.Email, request.FirstName, request.LastName, request.Active);
        }

        await members.SyncRoleProfilesAsync(member, request.Roles, request.Active, ct);
        return new ProvisionOrganizationMemberResponse(member.Id.Value, member.AuthGateUserId, request.Active ? "Active" : "Inactive");
    }
}
