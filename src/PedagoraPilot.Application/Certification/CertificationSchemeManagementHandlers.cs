using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Certification;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Certification;

// Every mutation is scoped to the actual referential version, not an untrusted tenant id.
internal static class SchemeManagementAccess
{
    internal static Task EnsureAsync(Guid versionId, ICurrentUser current,
        IReferentialVersionRepository versions, IReferentialRepository referentials,
        IProgramOfferingRepository offerings, ITrainingSiteRepository sites,
        ITrainingProgramRepository programs, CancellationToken ct) =>
        TenantCatalogAccess.EnsureReferentialVersionAsync(current, versionId,
            versions, referentials, offerings, sites, programs, ct, manage: true);
}

public sealed class CreateCertificationSchemeCommandHandler(
    ICertificationSchemeRepository schemes, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs, ICurrentUser current)
    : IRequestHandler<CreateCertificationSchemeCommand, CertificationSchemeDto>
{
    public async Task<CertificationSchemeDto> Handle(CreateCertificationSchemeCommand request, CancellationToken ct)
    {
        if (await versions.GetByIdAsync(request.ReferentialVersionId, false, ct) is null)
            throw new NotFoundApplicationException(ErrorKeys.ReferentialVersionNotFound);
        await SchemeManagementAccess.EnsureAsync(request.ReferentialVersionId, current,
            versions, referentials, offerings, sites, programs, ct);
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (await schemes.Query(false).AnyAsync(x => x.ReferentialVersionId == request.ReferentialVersionId && x.Code == code, ct))
            throw new ConflictApplicationException(ErrorKeys.CertificationSchemeAlreadyExists);
        var scheme = CertificationScheme.Create(request.ReferentialVersionId, code, request.Name);
        await schemes.AddAsync(scheme, ct);
        return CertMap.Scheme(scheme);
    }
}

public sealed class AddCertificationUnitCommandHandler(
    ICertificationSchemeRepository schemes, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs, ICurrentUser current)
    : IRequestHandler<AddCertificationUnitCommand, CertificationSchemeDto>
{
    public async Task<CertificationSchemeDto> Handle(AddCertificationUnitCommand request, CancellationToken ct)
    {
        var scheme = await schemes.Query(true).Include(x => x.Units).Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == request.SchemeId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        await SchemeManagementAccess.EnsureAsync(scheme.ReferentialVersionId, current,
            versions, referentials, offerings, sites, programs, ct);
        scheme.AddUnit(request.Code, request.Title, request.SortOrder);
        return CertMap.Scheme(scheme);
    }
}

public sealed class AddCertificationStepCommandHandler(
    ICertificationSchemeRepository schemes, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs, ICurrentUser current)
    : IRequestHandler<AddCertificationStepCommand, CertificationSchemeDto>
{
    public async Task<CertificationSchemeDto> Handle(AddCertificationStepCommand request, CancellationToken ct)
    {
        var scheme = await schemes.Query(true).Include(x => x.Units).Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == request.SchemeId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        await SchemeManagementAccess.EnsureAsync(scheme.ReferentialVersionId, current,
            versions, referentials, offerings, sites, programs, ct);
        if (!Enum.TryParse<CertificationStepKind>(request.Kind, true, out var kind)
            || !Enum.IsDefined(kind))
            throw new ValidationApplicationException(ErrorKeys.CertificationStepKindInvalid);
        scheme.AddStep(request.UnitId.HasValue ? new CertificationUnitId(request.UnitId.Value) : null,
            request.Code, request.Title, kind, request.DurationMinutes, request.SortOrder);
        return CertMap.Scheme(scheme);
    }
}

public sealed class PublishCertificationSchemeCommandHandler(
    ICertificationSchemeRepository schemes, IReferentialVersionRepository versions,
    IReferentialRepository referentials, IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites, ITrainingProgramRepository programs, ICurrentUser current)
    : IRequestHandler<PublishCertificationSchemeCommand, CertificationSchemeDto>
{
    public async Task<CertificationSchemeDto> Handle(PublishCertificationSchemeCommand request, CancellationToken ct)
    {
        var scheme = await schemes.Query(true).Include(x => x.Units).Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == request.SchemeId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        await SchemeManagementAccess.EnsureAsync(scheme.ReferentialVersionId, current,
            versions, referentials, offerings, sites, programs, ct);
        if (request.EffectiveFrom == default)
            throw new ValidationApplicationException(ErrorKeys.CertificationEffectiveDateRequired);
        scheme.Publish(request.EffectiveFrom, request.EffectiveTo);
        return CertMap.Scheme(scheme);
    }
}
