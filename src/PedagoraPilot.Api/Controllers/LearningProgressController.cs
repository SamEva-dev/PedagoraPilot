using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Learning.Progress;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/learning"), Authorize]
public sealed class LearningProgressController(IMediator mediator) : ControllerBase
{
    [HttpGet("referentials/{referentialVersionId:guid}/competencies")]
    [HasPermission(PedagoraPilotPermissionCodes.Skills.View)]
    public Task<IReadOnlyCollection<CompetencyDefinitionDto>> GetDefinitions(Guid referentialVersionId, CancellationToken ct) => mediator.Send(new GetCompetencyDefinitionsQuery(referentialVersionId), ct);
    [HttpGet("enrollments/{enrollmentId:guid}/competencies")]
    [HasPermission(PedagoraPilotPermissionCodes.Skills.View)]
    public Task<IReadOnlyCollection<LearnerCompetencyDto>> GetLearnerCompetencies(Guid enrollmentId, CancellationToken ct) => mediator.Send(new GetLearnerCompetenciesQuery(new EnrollmentId(enrollmentId)), ct);
    [HttpGet("cohorts/{cohortId:guid}/competencies")]
    [HasPermission(PedagoraPilotPermissionCodes.Skills.View)]
    public Task<IReadOnlyCollection<CohortCompetencyRowDto>> GetCohortCompetencies(Guid cohortId, CancellationToken ct) => mediator.Send(new GetCohortCompetenciesQuery(new CohortId(cohortId)), ct);
    [HttpPut("enrollments/{enrollmentId:guid}/competencies/{competencyDefinitionId:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Skills.Evaluate)]
    public Task<LearnerCompetencyDto> Evaluate(Guid enrollmentId, Guid competencyDefinitionId, [FromBody] EvaluateCompetencyRequest request, CancellationToken ct) => mediator.Send(new EvaluateCompetencyCommand(new EnrollmentId(enrollmentId), new CompetencyDefinitionId(competencyDefinitionId), request.Level, request.Score, request.Comment, request.EvaluatorAuthGateUserId, request.EvaluatorDisplayName, request.EvaluatedAtUtc), ct);
    [HttpGet("referentials/{referentialVersionId:guid}/topics")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.View)]
    public Task<IReadOnlyCollection<PedagogicalTopicDto>> GetTopics(Guid referentialVersionId, CancellationToken ct) => mediator.Send(new GetPedagogicalTopicsQuery(referentialVersionId), ct);
    [HttpGet("referentials/{referentialVersionId:guid}/topics/catalog")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.Manage)]
    public Task<IReadOnlyCollection<PedagogicalTopicDto>> GetTopicCatalog(Guid referentialVersionId, CancellationToken ct) => mediator.Send(new GetPedagogicalTopicCatalogQuery(referentialVersionId), ct);
    [HttpPost("referentials/{referentialVersionId:guid}/topics")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.Manage)]
    public Task<PedagogicalTopicDto> CreateTopic(Guid referentialVersionId, [FromBody] SavePedagogicalTopicRequest request, CancellationToken ct) => mediator.Send(new CreatePedagogicalTopicCommand(referentialVersionId, request.Number, request.Title, request.Category, request.DurationMinutes, request.Reference, request.Active, request.Objective, request.Example, request.Correction), ct);
    [HttpPut("referentials/{referentialVersionId:guid}/topics/{topicId:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.Manage)]
    public Task<PedagogicalTopicDto> UpdateTopicCatalog(Guid referentialVersionId, Guid topicId, [FromBody] SavePedagogicalTopicRequest request, CancellationToken ct) => mediator.Send(new UpdatePedagogicalTopicCommand(referentialVersionId, new PedagogicalTopicId(topicId), request.Number, request.Title, request.Category, request.DurationMinutes, request.Reference, request.Active, request.Objective, request.Example, request.Correction), ct);
    [HttpDelete("referentials/{referentialVersionId:guid}/topics/{topicId:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.Manage)]
    public Task<bool> DeleteTopic(Guid referentialVersionId, Guid topicId, CancellationToken ct) => mediator.Send(new DeletePedagogicalTopicCommand(referentialVersionId, new PedagogicalTopicId(topicId)), ct);
    [HttpGet("enrollments/{enrollmentId:guid}/topics")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.View)]
    public Task<IReadOnlyCollection<LearnerTopicProgressDto>> GetLearnerTopics(Guid enrollmentId, CancellationToken ct) => mediator.Send(new GetLearnerTopicsQuery(new EnrollmentId(enrollmentId)), ct);
    [HttpPut("enrollments/{enrollmentId:guid}/topics/{topicId:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Sheets.Manage)]
    public Task<LearnerTopicProgressDto> UpdateTopic(Guid enrollmentId, Guid topicId, [FromBody] UpdateTopicProgressRequest request, CancellationToken ct) => mediator.Send(new UpdateTopicProgressCommand(new EnrollmentId(enrollmentId), new PedagogicalTopicId(topicId), request.Status, request.PreparationDate, request.PresentationDate, request.PresentationDurationMinutes, request.EvaluatorDisplayName, request.PositivePoints, request.Improvements, request.Comment, request.NextObjective, request.EvaluationCriteria), ct);
    [HttpGet("enrollments/{enrollmentId:guid}/driving-evaluations")]
    [HasPermission(PedagoraPilotPermissionCodes.Driving.View)]
    public Task<IReadOnlyCollection<DrivingEvaluationDto>> GetDriving(Guid enrollmentId, CancellationToken ct) => mediator.Send(new GetDrivingEvaluationsQuery(new EnrollmentId(enrollmentId)), ct);
    [HttpPost("enrollments/{enrollmentId:guid}/driving-evaluations")]
    [HasPermission(PedagoraPilotPermissionCodes.Driving.Manage)]
    public Task<DrivingEvaluationDto> RecordDriving(Guid enrollmentId, [FromBody] RecordDrivingEvaluationRequest request, CancellationToken ct) => mediator.Send(new RecordDrivingEvaluationCommand(new EnrollmentId(enrollmentId), new CompetencyDefinitionId(request.CompetencyDefinitionId), request.TrainingSessionId.HasValue ? new TrainingSessionId(request.TrainingSessionId.Value) : null, request.EvaluatedAtUtc, request.TrainerAuthGateUserId, request.TrainerDisplayName, request.Subject, request.Positive, request.Difficulty, request.NextGoal, request.FreeObservation, request.Criteria), ct);
}
