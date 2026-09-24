using DomainRelay.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Application.Catalog.Referentials;
public sealed record GetReferentialsQuery(Guid? ProgramId = null) : IRequest<IReadOnlyCollection<ReferentialVersionDto>>;
public sealed class GetReferentialsQueryHandler(IReferentialRepository referentials, IReferentialVersionRepository versions, ITrainingProgramRepository programs) : IRequestHandler<GetReferentialsQuery, IReadOnlyCollection<ReferentialVersionDto>>
{
    public async Task<IReadOnlyCollection<ReferentialVersionDto>> Handle(GetReferentialsQuery q, CancellationToken ct)
    {
        var rs = await referentials.Query(false).Where(x => !q.ProgramId.HasValue || x.ProgramId == q.ProgramId).ToListAsync(ct);
        var ids = rs.Select(x => x.Id).ToArray();
        var vs = await versions.Query(false).Where(x => ids.Contains(x.ReferentialId)).OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        var caps = await versions.GetCapabilitiesAsync(vs.Select(x => x.Id), ct);
        var ps = await programs.Query(false).ToDictionaryAsync(x => x.Id, ct);
        return vs.Select(v => ToDto(v, rs.First(x => x.Id == v.ReferentialId), ps[rs.First(x => x.Id == v.ReferentialId).ProgramId], caps.GetValueOrDefault(v.Id, Array.Empty<string>()))).ToArray();
    }

    internal static ReferentialVersionDto ToDto(ReferentialVersion v, Referential r, TrainingProgram p, IReadOnlyCollection<string>? capabilities = null) => new(v.Id, v.ExternalKey ?? v.Id.ToString(), r.Id, r.ProgramId, p.ExternalKey ?? p.Id.ToString(), r.Code, r.Name, v.VersionLabel, v.CertificationCode, v.Status.ToString().ToLowerInvariant(), v.EffectiveFrom, v.EffectiveTo, v.TotalHours, v.SheetCount, v.RequiredDocumentCount, capabilities ?? v.Capabilities, v.NotesKey);
}

public sealed class CreateReferentialVersionCommandHandler(IReferentialRepository refs, IReferentialVersionRepository versions, ITrainingProgramRepository programs) : IRequestHandler<CreateReferentialVersionCommand, ReferentialVersionDto>
{
    public async Task<ReferentialVersionDto> Handle(CreateReferentialVersionCommand r, CancellationToken ct)
    {
        var rf = await refs.GetByIdAsync(r.ReferentialId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialNotFound);
        if (await versions.VersionExistsAsync(r.ReferentialId, r.Version, ct))
            throw new ConflictApplicationException(ErrorKeys.ReferentialVersionAlreadyExists);
        var p = await programs.GetByIdAsync(rf.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var v = ReferentialVersion.CreateDraft(r.ReferentialId, r.Version, r.CertificationCode, r.EffectiveFrom, r.TotalHours, r.SheetCount, r.RequiredDocumentCount, r.EnabledModules, r.NotesKey, r.ExternalKey);
        await versions.AddAsync(v, ct);
        return GetReferentialsQueryHandler.ToDto(v, rf, p);
    }
}

public sealed class PublishReferentialVersionCommandHandler(IReferentialRepository refs, IReferentialVersionRepository versions, ITrainingProgramRepository programs) : IRequestHandler<PublishReferentialVersionCommand, ReferentialVersionDto>
{
    public async Task<ReferentialVersionDto> Handle(PublishReferentialVersionCommand r, CancellationToken ct)
    {
        var v = await versions.GetByIdAsync(r.VersionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialVersionNotFound);
        var rf = await refs.GetByIdAsync(v.ReferentialId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ReferentialNotFound);
        var p = await programs.GetByIdAsync(rf.ProgramId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.ProgramNotFound);
        var actives = await versions.Query(true).Where(x => x.ReferentialId == v.ReferentialId && x.Status == ReferentialVersionStatus.Active && x.Id != v.Id).ToListAsync(ct);
        foreach (var old in actives)
            old.Archive(v.EffectiveFrom.AddDays(-1));
        v.Publish(DateTime.UtcNow);
        return GetReferentialsQueryHandler.ToDto(v, rf, p);
    }
}
