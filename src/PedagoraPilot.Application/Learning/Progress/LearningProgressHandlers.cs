using PedagoraPilot.Application.Abstractions.Security;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;
using PedagoraPilot.Application.Training.Learners;

namespace PedagoraPilot.Application.Learning.Progress;
internal static class LearningProgressDtoFactory
{
    public static CompetencyDefinitionDto Definition(CompetencyDefinition x, IObjectMapper mapper)
    {
        var m = mapper.Map<CompetencyDefinition, CompetencyDefinitionReadModel>(x);
        return new(m.Id.Value, m.ReferentialVersionId, m.ParentId?.Value, m.Code, m.Title, m.Kind.ToString().ToLowerInvariant(), m.SortOrder, m.Active);
    }

    public static LearnerCompetencyDto Competency(LearnerCompetencyRecord x, CompetencyDefinition d, IObjectMapper mapper)
    {
        var m = mapper.Map<LearnerCompetencyRecord, LearnerCompetencyRecordReadModel>(x);
        return new(m.Id.Value, m.EnrollmentId.Value, m.CompetencyDefinitionId.Value, d.Code, d.Title, LevelCode(m.Level), m.Score, m.Comment, m.EvaluatorDisplayName, m.EvaluatedAtUtc);
    }

    public static PedagogicalTopicDto Topic(PedagogicalTopic x, IObjectMapper mapper)
    {
        var m = mapper.Map<PedagogicalTopic, PedagogicalTopicReadModel>(x);
        return new(m.Id.Value, m.ReferentialVersionId, m.Code, m.Number, m.Title, m.Category, m.DurationMinutes, m.Reference, m.Active, m.Objective, m.Example, m.Correction);
    }

    public static LearnerTopicProgressDto TopicProgress(LearnerTopicProgress p, PedagogicalTopic t, IObjectMapper mapper)
    {
        var m = mapper.Map<LearnerTopicProgress, LearnerTopicProgressReadModel>(p);
        return new(
            m.Id.Value,
            m.EnrollmentId.Value,
            m.TopicId.Value,
            t.Code,
            t.Number,
            t.Title,
            t.Category,
            TopicStatusCode(m.Status),
            m.PreparationDate,
            m.PresentationDate,
            m.PresentationDurationMinutes,
            m.EvaluatorDisplayName,
            m.PositivePoints,
            m.Improvements,
            m.Comment,
            m.NextObjective,
            p.EvaluationCriteria
                .OrderBy(x => x.Code)
                .Select(x => new TopicEvaluationCriterionDto(x.Id.Value, x.Code, TopicEvaluationLevelCode(x.Level)))
                .ToArray());
    }

    public static DrivingEvaluationDto Driving(DrivingEvaluation x, IObjectMapper mapper)
    {
        var m = mapper.Map<DrivingEvaluation, DrivingEvaluationReadModel>(x);
        return new(m.Id.Value, m.EnrollmentId.Value, m.CompetencyDefinitionId.Value, m.TrainingSessionId?.Value, m.EvaluatedAtUtc, m.TrainerAuthGateUserId, m.TrainerDisplayName, m.Subject, m.Positive, m.Difficulty, m.NextGoal, m.FreeObservation, x.Criteria.Select(c => new DrivingCriterionDto(c.Id.Value, c.Code, c.Label, LevelCode(c.Level))).ToArray());
    }

    public static string LevelCode(CompetencyLevel level) => level switch
    {
        CompetencyLevel.NotAssessed => "not_assessed",
        CompetencyLevel.InProgress => "in_progress",
        CompetencyLevel.Rework => "rework",
        CompetencyLevel.Acquired => "acquired",
        _ => "not_assessed"
    };
    public static string TopicStatusCode(TopicProgressStatus status) => status switch
    {
        TopicProgressStatus.NotStarted => "not_started",
        TopicProgressStatus.InProgress => "in_progress",
        TopicProgressStatus.Ready => "ready",
        TopicProgressStatus.Presented => "presented",
        TopicProgressStatus.Validated => "validated",
        TopicProgressStatus.Rework => "rework",
        _ => "not_started"
    };
    public static string TopicEvaluationLevelCode(TopicEvaluationLevel level) => level switch
    {
        TopicEvaluationLevel.Acquired => "acquired",
        TopicEvaluationLevel.InProgress => "in_progress",
        TopicEvaluationLevel.Review => "review",
        _ => "in_progress"
    };
    public static bool TryParseLevel(string value, out CompetencyLevel level) => Enum.TryParse((value ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty), true, out level);
    public static bool TryParseTopicStatus(string value, out TopicProgressStatus status) => Enum.TryParse((value ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty), true, out status);
    public static bool TryParseTopicEvaluationLevel(string value, out TopicEvaluationLevel level) => Enum.TryParse((value ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty), true, out level);
}

public sealed class GetCompetencyDefinitionsQueryHandler(ICompetencyDefinitionRepository definitions, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetCompetencyDefinitionsQuery, IReadOnlyCollection<CompetencyDefinitionDto>>
{
    public async Task<IReadOnlyCollection<CompetencyDefinitionDto>> Handle(GetCompetencyDefinitionsQuery r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct);
        return (await definitions.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Active).OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(ct)).Select(x => LearningProgressDtoFactory.Definition(x, mapper)).ToArray();
    }
}


public sealed class GetCompetencyDefinitionCatalogQueryHandler(ICompetencyDefinitionRepository definitions, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetCompetencyDefinitionCatalogQuery, IReadOnlyCollection<CompetencyDefinitionDto>>
{
    public async Task<IReadOnlyCollection<CompetencyDefinitionDto>> Handle(GetCompetencyDefinitionCatalogQuery r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        return (await definitions.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId).OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(ct)).Select(x => LearningProgressDtoFactory.Definition(x, mapper)).ToArray();
    }
}

public sealed class CreateCompetencyDefinitionCommandHandler(ICompetencyDefinitionRepository definitions, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<CreateCompetencyDefinitionCommand, CompetencyDefinitionDto>
{
    public async Task<CompetencyDefinitionDto> Handle(CreateCompetencyDefinitionCommand r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        var code = (r.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (await definitions.Query(false).AnyAsync(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Code == code, ct))
            throw new ConflictApplicationException(ErrorKeys.CompetencyCodeConflict);
        if (!Enum.TryParse<CompetencyKind>((r.Kind ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty), true, out var kind))
            throw new ValidationApplicationException(ErrorKeys.CompetencyKindInvalid);
        CompetencyDefinitionId? parentId = null;
        if (r.ParentId.HasValue)
        {
            var parent = await definitions.GetByIdAsync(new CompetencyDefinitionId(r.ParentId.Value), false, ct)
                ?? throw new ValidationApplicationException(ErrorKeys.CompetencyParentInvalid);
            if (parent.ReferentialVersionId != r.ReferentialVersionId || parent.ParentId.HasValue)
                throw new ValidationApplicationException(ErrorKeys.CompetencyParentInvalid);
            parentId = parent.Id;
        }
        var entity = CompetencyDefinition.Create(r.ReferentialVersionId, parentId, code, r.Title, kind, r.SortOrder, r.Active);
        await definitions.AddAsync(entity, ct);
        return LearningProgressDtoFactory.Definition(entity, mapper);
    }
}

public sealed class UpdateCompetencyDefinitionCommandHandler(ICompetencyDefinitionRepository definitions, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<UpdateCompetencyDefinitionCommand, CompetencyDefinitionDto>
{
    public async Task<CompetencyDefinitionDto> Handle(UpdateCompetencyDefinitionCommand r, CancellationToken ct)
    {
        var entity = await definitions.GetByIdAsync(r.CompetencyDefinitionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CompetencyDefinitionNotFound);
        if (entity.ReferentialVersionId != r.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.CompetencyReferentialMismatch);
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        var code = (r.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (await definitions.Query(false).AnyAsync(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Code == code && x.Id != entity.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.CompetencyCodeConflict);
        if (!Enum.TryParse<CompetencyKind>((r.Kind ?? string.Empty).Replace("_", string.Empty).Replace("-", string.Empty), true, out var kind))
            throw new ValidationApplicationException(ErrorKeys.CompetencyKindInvalid);
        CompetencyDefinitionId? parentId = null;
        if (r.ParentId.HasValue)
        {
            var requested = new CompetencyDefinitionId(r.ParentId.Value);
            if (requested == entity.Id)
                throw new ValidationApplicationException(ErrorKeys.CompetencyParentInvalid);
            var parent = await definitions.GetByIdAsync(requested, false, ct) ?? throw new ValidationApplicationException(ErrorKeys.CompetencyParentInvalid);
            if (parent.ReferentialVersionId != r.ReferentialVersionId || parent.ParentId.HasValue)
                throw new ValidationApplicationException(ErrorKeys.CompetencyParentInvalid);
            parentId = parent.Id;
        }
        entity.Update(parentId, code, r.Title, kind, r.SortOrder, r.Active);
        return LearningProgressDtoFactory.Definition(entity, mapper);
    }
}

public sealed class DeleteCompetencyDefinitionCommandHandler(ICompetencyDefinitionRepository definitions, ILearnerCompetencyRecordRepository records, IDrivingEvaluationRepository driving, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<DeleteCompetencyDefinitionCommand, bool>
{
    public async Task<bool> Handle(DeleteCompetencyDefinitionCommand r, CancellationToken ct)
    {
        var entity = await definitions.GetByIdAsync(r.CompetencyDefinitionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CompetencyDefinitionNotFound);
        if (entity.ReferentialVersionId != r.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.CompetencyReferentialMismatch);
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        if (await definitions.Query(false).AnyAsync(x => x.ParentId == entity.Id, ct)
            || await records.Query(false).AnyAsync(x => x.CompetencyDefinitionId == entity.Id, ct)
            || await driving.Query(false).AnyAsync(x => x.CompetencyDefinitionId == entity.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.CompetencyInUse);
        definitions.Remove(entity);
        return true;
    }
}

public sealed class GetLearnerCompetenciesQueryHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICompetencyDefinitionRepository definitions, ILearnerCompetencyRecordRepository records, ILearnerProfileRepository profiles, IPersonRepository people, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetLearnerCompetenciesQuery, IReadOnlyCollection<LearnerCompetencyDto>>
{
    public async Task<IReadOnlyCollection<LearnerCompetencyDto>> Handle(GetLearnerCompetenciesQuery r, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        await LearnerSelfAccess.EnsureAsync(enrollment, profiles, people, current, ct);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
        var defs = await definitions.Query(false).Where(x => x.ReferentialVersionId == cohort.ReferentialVersionId && x.Active).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var rows = await records.Query(false).Where(x => x.EnrollmentId == r.EnrollmentId).ToListAsync(ct);
        var byDefinition = rows.ToDictionary(x => x.CompetencyDefinitionId);
        return defs.Select(d =>
        {
            byDefinition.TryGetValue(d.Id, out var row);
            return row is null ? new LearnerCompetencyDto(Guid.Empty, enrollment.Id.Value, d.Id.Value, d.Code, d.Title, LearningProgressDtoFactory.LevelCode(CompetencyLevel.NotAssessed), null, null, null, null) : LearningProgressDtoFactory.Competency(row, d, mapper);
        }).ToArray();
    }
}

public sealed class EvaluateCompetencyCommandHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICompetencyDefinitionRepository definitions, ILearnerCompetencyRecordRepository records, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<EvaluateCompetencyCommand, LearnerCompetencyDto>
{
    public async Task<LearnerCompetencyDto> Handle(EvaluateCompetencyCommand r, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        if (enrollment.Status != EnrollmentStatus.Active)
            throw new ConflictApplicationException(ErrorKeys.CompetencyEnrollmentNotActive);
        var definition = await definitions.GetByIdAsync(r.CompetencyDefinitionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CompetencyDefinitionNotFound);
        if (definition.ReferentialVersionId != cohort.ReferentialVersionId || !definition.Active)
            throw new ConflictApplicationException(ErrorKeys.CompetencyReferentialMismatch);
        if (!LearningProgressDtoFactory.TryParseLevel(r.Level, out var level))
            throw new ValidationApplicationException(ErrorKeys.CompetencyLevelInvalid);
        var record = await records.FindAsync(r.EnrollmentId, r.CompetencyDefinitionId, true, ct);
        if (record is null)
        {
            record = LearnerCompetencyRecord.Create(enrollment.OrganizationId, enrollment.Id, definition.Id);
            await records.AddAsync(record, ct);
        }

        record.Evaluate(level, r.Score, r.Comment, r.EvaluatorAuthGateUserId, r.EvaluatorDisplayName, r.EvaluatedAtUtc ?? DateTimeOffset.UtcNow);
        return LearningProgressDtoFactory.Competency(record, definition, mapper);
    }
}

public sealed class GetPedagogicalTopicsQueryHandler(IPedagogicalTopicRepository topics, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetPedagogicalTopicsQuery, IReadOnlyCollection<PedagogicalTopicDto>>
{
    public async Task<IReadOnlyCollection<PedagogicalTopicDto>> Handle(GetPedagogicalTopicsQuery r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct);
        return (await topics.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Active).OrderBy(x => x.Number).ThenBy(x => x.Code).ToListAsync(ct)).Select(x => LearningProgressDtoFactory.Topic(x, mapper)).ToArray();
    }
}

public sealed class GetPedagogicalTopicCatalogQueryHandler(IPedagogicalTopicRepository topics, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetPedagogicalTopicCatalogQuery, IReadOnlyCollection<PedagogicalTopicDto>>
{
    public async Task<IReadOnlyCollection<PedagogicalTopicDto>> Handle(GetPedagogicalTopicCatalogQuery r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct);
        return (await topics.Query(false).Where(x => x.ReferentialVersionId == r.ReferentialVersionId).OrderBy(x => x.Number).ThenBy(x => x.Code).ToListAsync(ct)).Select(x => LearningProgressDtoFactory.Topic(x, mapper)).ToArray();
    }
}

public sealed class CreatePedagogicalTopicCommandHandler(IPedagogicalTopicRepository topics, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<CreatePedagogicalTopicCommand, PedagogicalTopicDto>
{
    public async Task<PedagogicalTopicDto> Handle(CreatePedagogicalTopicCommand r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        if (await topics.Query(false).AnyAsync(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Number == r.Number, ct))
            throw new ConflictApplicationException(ErrorKeys.PedagogicalTopicNumberConflict);

        var code = $"SHEET-{r.Number:D3}";
        if (await topics.Query(false).AnyAsync(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Code == code, ct))
            code = $"SHEET-{r.Number:D3}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

        var entity = PedagogicalTopic.Create(r.ReferentialVersionId, code, r.Number, r.Title, r.Category, r.DurationMinutes, r.Reference, r.Active, null, r.Objective, r.Example, r.Correction);
        await topics.AddAsync(entity, ct);
        return LearningProgressDtoFactory.Topic(entity, mapper);
    }
}

public sealed class ImportTpEcsr58TopicsCommandHandler(
    IPedagogicalTopicRepository topics,
    IObjectMapper mapper,
    ICurrentUser current,
    IReferentialVersionRepository versions,
    IReferentialRepository referentials,
    IProgramOfferingRepository offerings,
    ITrainingSiteRepository sites) : IRequestHandler<ImportTpEcsr58TopicsCommand, IReadOnlyCollection<PedagogicalTopicDto>>
{
    private sealed record SeedTopic(int Number, string Title, string Category);

    private static readonly SeedTopic[] Catalog =
    {
        new(1, "Le continuum éducatif. Formation obligatoire et facultative", "pedagogy"),
        new(2, "Le permis de conduire : catégorie B, définition, condition d’obtention, période probatoire", "rules"),
        new(3, "Permis de conduire : sanctions administratives et pénales", "rules"),
        new(4, "Le permis à points, son rôle, son fonctionnement", "rules"),
        new(5, "Les documents et équipements obligatoires et facultatifs du conducteur", "rules"),
        new(6, "L’assurance automobile : le rôle de l’assurance. Assurance obligatoire et facultative", "rules"),
        new(7, "L’assurance automobile : le constat à l’amiable", "rules"),
        new(8, "Le risque routier chez les jeunes entre 18 et 24 ans", "risk"),
        new(9, "L’âge et la conduite : classe d’âge, sexe et risques", "risk"),
        new(10, "Pressions et influences incitant à une vitesse inadaptée", "risk"),
        new(11, "L’alcool : effets et accidentologie", "risk"),
        new(12, "L’alcool : le cadre réglementaire", "rules"),
        new(13, "Alcool : diffusion et élimination", "risk"),
        new(14, "Estimation du taux d’alcoolémie", "risk"),
        new(15, "Les substances psychoactives (sauf l’alcool)", "risk"),
        new(16, "L’attention du conducteur : vigilance, attention et fatigue", "risk"),
        new(17, "La vue du conducteur : fonctionnement de l’œil (acuité visuelle, champ visuel, tache de Mariotte, …), réglementation", "risk"),
        new(18, "Les systèmes de retenue : rôle, réglementation, accidentologie, installations des passagers", "vehicle"),
        new(19, "L’utilisation rationnelle du véhicule (aspect mécanique)", "vehicle"),
        new(20, "L’éco conduite", "mobility"),
        new(21, "Le temps de réaction et distances de sécurité", "risk"),
        new(22, "Les dispositifs de sécurité active : fonctions et effets pervers", "vehicle"),
        new(23, "Aides et assistances à la conduite : définitions, rôles et homéostasie du risque", "vehicle"),
        new(24, "Les vérifications de sécurité du véhicule", "vehicle"),
        new(25, "Feux obligatoires et facultatifs des véhicules", "vehicle"),
        new(26, "Les surfaces vitrées et les rétroviseurs : descriptions, leurs usages, l’entretien", "vehicle"),
        new(27, "Lois physiques appliquées aux véhicules : identification et conséquences", "vehicle"),
        new(28, "Le transport d’une charge en sécurité à l’aide du véhicule", "vehicle"),
        new(29, "Les pneumatiques : adhérence, entretien et réglementation", "vehicle"),
        new(30, "Les ensembles de véhicule de la catégorie B : réglementation, signalisation et comportement", "vehicle"),
        new(31, "La signalisation verticale", "rules"),
        new(32, "La signalisation horizontale", "rules"),
        new(33, "Les intersections gérées par une signalisation lumineuse", "rules"),
        new(34, "Les intersections non gérées par une signalisation lumineuse", "rules"),
        new(35, "Les ronds-points et les sens giratoires", "rules"),
        new(36, "Les passages à niveaux", "rules"),
        new(37, "Stationnement, arrêt, immobilisation", "rules"),
        new(38, "Les voies réservées", "rules"),
        new(39, "Les croisements", "rules"),
        new(40, "Les dépassements", "rules"),
        new(41, "Vitesse : la réglementation et les sanctions", "rules"),
        new(42, "Vitesse : la signalisation et son rôle", "rules"),
        new(43, "Les distances d’arrêt", "risk"),
        new(44, "Les usagers vulnérables", "mobility"),
        new(45, "Le partage social de la route avec les piétons", "mobility"),
        new(46, "Le partage social avec les 2 roues et L5e", "mobility"),
        new(47, "Le partage social avec les véhicules du groupe lourd", "mobility"),
        new(48, "Véhicules d’intérêt général", "rules"),
        new(49, "La fonction sociale de la réglementation en matière de S.R.", "pedagogy"),
        new(50, "La communication entre les usagers de la route", "mobility"),
        new(51, "Conduite et préparation d’un voyage", "mobility"),
        new(52, "Circulation en hiver", "risk"),
        new(53, "Les risques à la conduite par intempéries", "risk"),
        new(54, "Les risques à la conduite de nuit", "risk"),
        new(55, "L’autoroute", "rules"),
        new(56, "Approche factorielle de l’accident, système HEVO", "risk"),
        new(57, "Le comportement en cas d’accident corporel et matériel", "rules"),
        new(58, "Les justifications des politiques de S.R.", "pedagogy"),
    };

    public async Task<IReadOnlyCollection<PedagogicalTopicDto>> Handle(ImportTpEcsr58TopicsCommand r, CancellationToken ct)
    {
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, r.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);

        var existingNumbers = (await topics.Query(false)
            .Where(x => x.ReferentialVersionId == r.ReferentialVersionId && x.Number.HasValue)
            .Select(x => x.Number!.Value)
            .ToListAsync(ct))
            .ToHashSet();

        foreach (var seed in Catalog.Where(x => !existingNumbers.Contains(x.Number)))
        {
            var entity = PedagogicalTopic.Create(
                r.ReferentialVersionId,
                $"SHEET-{seed.Number:D3}",
                seed.Number,
                seed.Title,
                seed.Category,
                20,
                "Liste officielle des 58 fiches pédagogiques TP ECSR",
                true,
                null,
                null,
                null,
                null);
            await topics.AddAsync(entity, ct);
        }

        return (await topics.Query(false)
            .Where(x => x.ReferentialVersionId == r.ReferentialVersionId)
            .OrderBy(x => x.Number)
            .ThenBy(x => x.Code)
            .ToListAsync(ct))
            .Select(x => LearningProgressDtoFactory.Topic(x, mapper))
            .ToArray();
    }
}

public sealed class UpdatePedagogicalTopicCommandHandler(IPedagogicalTopicRepository topics, IObjectMapper mapper, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<UpdatePedagogicalTopicCommand, PedagogicalTopicDto>
{
    public async Task<PedagogicalTopicDto> Handle(UpdatePedagogicalTopicCommand r, CancellationToken ct)
    {
        var entity = await topics.GetByIdAsync(r.TopicId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PedagogicalTopicNotFound);
        if (entity.ReferentialVersionId != r.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.TopicReferentialMismatch);
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, entity.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        if (await topics.Query(false).AnyAsync(x => x.ReferentialVersionId == entity.ReferentialVersionId && x.Number == r.Number && x.Id != entity.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.PedagogicalTopicNumberConflict);

        entity.UpdateCatalog(r.Number, r.Title, r.Category, r.DurationMinutes, r.Reference, r.Active, r.Objective, r.Example, r.Correction);
        return LearningProgressDtoFactory.Topic(entity, mapper);
    }
}

public sealed class DeletePedagogicalTopicCommandHandler(IPedagogicalTopicRepository topics, ILearnerTopicProgressRepository progress, ICurrentUser current, IReferentialVersionRepository versions, IReferentialRepository referentials, IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<DeletePedagogicalTopicCommand, bool>
{
    public async Task<bool> Handle(DeletePedagogicalTopicCommand r, CancellationToken ct)
    {
        var entity = await topics.GetByIdAsync(r.TopicId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PedagogicalTopicNotFound);
        if (entity.ReferentialVersionId != r.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.TopicReferentialMismatch);
        await TenantCatalogAccess.EnsureReferentialVersionAsync(current, entity.ReferentialVersionId, versions, referentials, offerings, sites, ct, manage: true);
        if (await progress.Query(false).AnyAsync(x => x.TopicId == entity.Id, ct))
            throw new ConflictApplicationException(ErrorKeys.PedagogicalTopicInUse);
        topics.Remove(entity);
        return true;
    }
}

public sealed class GetLearnerTopicsQueryHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, IPedagogicalTopicRepository topics, ILearnerTopicProgressRepository progress, ILearnerProfileRepository profiles, IPersonRepository people, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetLearnerTopicsQuery, IReadOnlyCollection<LearnerTopicProgressDto>>
{
    public async Task<IReadOnlyCollection<LearnerTopicProgressDto>> Handle(GetLearnerTopicsQuery r, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        await LearnerSelfAccess.EnsureAsync(enrollment, profiles, people, current, ct);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
        var topicRows = await topics.Query(false).Where(x => x.ReferentialVersionId == cohort.ReferentialVersionId && x.Active).OrderBy(x => x.Number).ToListAsync(ct);
        var saved = await progress.Query(false)
            .Where(x => x.EnrollmentId == r.EnrollmentId)
            .Include(x => x.EvaluationCriteria)
            .ToListAsync(ct);
        var byTopic = saved.ToDictionary(x => x.TopicId);
        return topicRows.Select(t =>
        {
            byTopic.TryGetValue(t.Id, out var p);
            return p is null
                ? new LearnerTopicProgressDto(
                    Guid.Empty,
                    enrollment.Id.Value,
                    t.Id.Value,
                    t.Code,
                    t.Number,
                    t.Title,
                    t.Category,
                    LearningProgressDtoFactory.TopicStatusCode(TopicProgressStatus.NotStarted),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<TopicEvaluationCriterionDto>())
                : LearningProgressDtoFactory.TopicProgress(p, t, mapper);
        }).ToArray();
    }
}

public sealed class UpdateTopicProgressCommandHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, IPedagogicalTopicRepository topics, ILearnerTopicProgressRepository progress, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<UpdateTopicProgressCommand, LearnerTopicProgressDto>
{
    public async Task<LearnerTopicProgressDto> Handle(UpdateTopicProgressCommand r, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        var topic = await topics.GetByIdAsync(r.TopicId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PedagogicalTopicNotFound);
        if (topic.ReferentialVersionId != cohort.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.TopicReferentialMismatch);
        if (!LearningProgressDtoFactory.TryParseTopicStatus(r.Status, out var status))
            throw new ValidationApplicationException(ErrorKeys.TopicStatusInvalid);
        var criteria = new List<(string Code, TopicEvaluationLevel Level)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var criterion in r.EvaluationCriteria ?? [])
        {
            if (criterion is null || string.IsNullOrWhiteSpace(criterion.Code))
                throw new ValidationApplicationException(ErrorKeys.TopicEvaluationCriteriaInvalid);

            var code = criterion.Code.Trim();
            if (!seen.Add(code) || !LearningProgressDtoFactory.TryParseTopicEvaluationLevel(criterion.Level, out var level))
                throw new ValidationApplicationException(ErrorKeys.TopicEvaluationCriteriaInvalid);

            criteria.Add((code, level));
        }

        var entity = await progress.FindAsync(r.EnrollmentId, r.TopicId, true, ct);
        if (entity is null)
        {
            entity = LearnerTopicProgress.Create(enrollment.OrganizationId, enrollment.Id, topic.Id);
            await progress.AddAsync(entity, ct);
        }

        // The evaluator identity comes from the authenticated token, never from the request body.
        entity.Update(
            status,
            r.PreparationDate,
            r.PresentationDate,
            r.PresentationDurationMinutes,
            current.DisplayName ?? current.Email,
            r.PositivePoints,
            r.Improvements,
            r.Comment,
            r.NextObjective,
            criteria);
        return LearningProgressDtoFactory.TopicProgress(entity, topic, mapper);
    }
}

public sealed class GetDrivingEvaluationsQueryHandler(IDrivingEvaluationRepository evaluations, IObjectMapper mapper, IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, ILearnerProfileRepository profiles, IPersonRepository people, ICurrentUser current) : IRequestHandler<GetDrivingEvaluationsQuery, IReadOnlyCollection<DrivingEvaluationDto>>
{
    public async Task<IReadOnlyCollection<DrivingEvaluationDto>> Handle(GetDrivingEvaluationsQuery r, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        await LearnerSelfAccess.EnsureAsync(enrollment, profiles, people, current, ct);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
        var query = evaluations.Query(false).Where(x => x.EnrollmentId == r.EnrollmentId);
        if (current.Roles.Contains("PedagoraPilot.Trainer")
            && !current.HasPermission(PedagoraPilot.Security.Contracts.PedagoraPilotPermissionCodes.Driving.ViewOthers))
        {
            var ownTrainerId = current.UserId?.ToString("D");
            if (string.IsNullOrWhiteSpace(ownTrainerId)) return Array.Empty<DrivingEvaluationDto>();
            query = query.Where(x => x.TrainerAuthGateUserId == ownTrainerId);
        }
        return (await query.Include(x => x.Criteria).OrderByDescending(x => x.EvaluatedAtUtc).ToListAsync(ct))
            .Select(x => LearningProgressDtoFactory.Driving(x, mapper)).ToArray();
    }
}

public sealed class RecordDrivingEvaluationCommandHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICompetencyDefinitionRepository definitions, ITrainingSessionRepository sessions, IDrivingEvaluationRepository evaluations, IOrganizationMemberRepository members, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<RecordDrivingEvaluationCommand, DrivingEvaluationDto>
{
    public async Task<DrivingEvaluationDto> Handle(RecordDrivingEvaluationCommand r, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        var definition = await definitions.GetByIdAsync(r.CompetencyDefinitionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CompetencyDefinitionNotFound);
        if (definition.ReferentialVersionId != cohort.ReferentialVersionId || !definition.Active)
            throw new ConflictApplicationException(ErrorKeys.CompetencyReferentialMismatch);
        if (r.TrainingSessionId.HasValue)
        {
            var s = await sessions.GetByIdAsync(r.TrainingSessionId.Value, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
            if (s.OrganizationId != enrollment.OrganizationId || s.CohortId != enrollment.CohortId)
                throw new ConflictApplicationException(ErrorKeys.DrivingSessionCohortMismatch);
            if (s.Type != TrainingSessionType.Driving || s.Status == TrainingSessionStatus.Cancelled
                || s.StartsAtUtc > DateTimeOffset.UtcNow
                || (s.AudienceMode == SessionAudienceMode.SelectedEnrollments
                    && !s.Participants.Any(p => p.EnrollmentId == enrollment.Id)))
                throw new ValidationApplicationException(ErrorKeys.DrivingSessionInvalid);
        }

        var childDefinitions = await definitions.Query(false)
            .Where(x => x.ReferentialVersionId == cohort.ReferentialVersionId && x.ParentId == definition.Id && x.Active)
            .ToListAsync(ct);
        var allowed = (childDefinitions.Count > 0 ? childDefinitions : [definition])
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        if (r.Criteria is null || r.Criteria.Count == 0 || r.Criteria.Count > allowed.Count)
            throw new ValidationApplicationException(ErrorKeys.DrivingCriteriaInvalid);
        var criteria = new List<(string, string, CompetencyLevel)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in r.Criteria)
        {
            if (c is null || string.IsNullOrWhiteSpace(c.Code)
                || !seen.Add(c.Code.Trim()) || !allowed.TryGetValue(c.Code.Trim(), out var criterion))
                throw new ValidationApplicationException(ErrorKeys.DrivingCriteriaInvalid);
            if (!LearningProgressDtoFactory.TryParseLevel(c.Level, out var level)
                || !Enum.IsDefined(level))
                throw new ValidationApplicationException(ErrorKeys.CompetencyLevelInvalid);
            criteria.Add((criterion.Code, criterion.Title, level));
        }

        var trainer = await PedagoraPilot.Application.Training.Delivery.CreateTrainingSessionCommandHandler.VerifiedTrainerAsync(
            current, r.TrainerAuthGateUserId, r.TrainerDisplayName, members, enrollment.OrganizationId,
            PedagoraPilot.Security.Contracts.PedagoraPilotPermissionCodes.Driving.AssignTrainer, ct);
        if (string.IsNullOrWhiteSpace(trainer.Name))
            throw new ValidationApplicationException(ErrorKeys.DrivingTrainerRequired);
        var entity = DrivingEvaluation.Create(enrollment.OrganizationId, enrollment.Id, definition.Id, r.TrainingSessionId,
            DateTimeOffset.UtcNow, trainer.Id, trainer.Name, r.Subject,
            r.Positive, r.Difficulty, r.NextGoal, r.FreeObservation, criteria);
        await evaluations.AddAsync(entity, ct);
        return LearningProgressDtoFactory.Driving(entity, mapper);
    }
}
