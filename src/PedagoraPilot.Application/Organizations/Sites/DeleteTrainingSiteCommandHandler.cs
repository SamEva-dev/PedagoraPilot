using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;

namespace PedagoraPilot.Application.Organizations.Sites;

public sealed class DeleteTrainingSiteCommandHandler(
    ITrainingSiteRepository sites,
    IProgramOfferingRepository offerings,
    ICohortRepository cohorts,
    ITrainingSessionRepository sessions,
    IDistanceLearningSessionRepository distanceSessions,
    IAsyncLearningModuleRepository asyncModules,
    ICertificationExamSessionRepository examSessions,
    IRemoteWorkRequestRepository remoteWork,
    IDocumentRepository documents,
    ICurrentUser current) : IRequestHandler<DeleteTrainingSiteCommand, bool>
{
    public async Task<bool> Handle(DeleteTrainingSiteCommand request, CancellationToken ct)
    {
        TenantScope.Ensure(current, request.OrganizationId);
        var site = await sites.GetByIdAsync(request.SiteId, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
        if (site.OrganizationId != request.OrganizationId)
            throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);

        ContextualScope.EnsureCanManageSite(current, site.Id);

        var hasOperationalData =
            await cohorts.Query(false).AnyAsync(x => x.SiteId == site.Id, ct)
            || await sessions.Query(false).AnyAsync(x => x.SiteId == site.Id, ct)
            || await distanceSessions.Query(false).AnyAsync(x => x.SiteId == site.Id, ct)
            || await asyncModules.Query(false).AnyAsync(x => x.SiteId == site.Id, ct)
            || await examSessions.Query(false).AnyAsync(x => x.SiteId == site.Id, ct)
            || await remoteWork.Query(false).AnyAsync(x => x.SiteId == site.Id, ct)
            || await documents.Query(false).AnyAsync(x => x.SiteId == site.Id, ct);

        if (hasOperationalData)
            throw new ConflictApplicationException(ErrorKeys.SiteDeleteInUse);

        var siteOfferings = await offerings.Query(true)
            .Where(x => x.SiteId == site.Id)
            .ToListAsync(ct);
        foreach (var offering in siteOfferings) offerings.Remove(offering);

        site.MarkDeleted();
        sites.Remove(site);
        return true;
    }
}
