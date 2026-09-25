using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/learners"), Authorize]
public sealed class LearnersController(IMediator mediator) : ControllerBase
{
    [HttpGet("me"), HasPermission(PedagoraPilotPermissionCodes.Learners.DetailView)]
    public Task<LearnerDto> GetSelf(CancellationToken ct) => mediator.Send(new GetSelfLearnerQuery(), ct);
    [HttpGet("{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Learners.DetailView)]
    public Task<LearnerDto> Get(Guid id, CancellationToken ct) => mediator.Send(new GetLearnerQuery(new LearnerProfileId(id)), ct);
    [HttpGet("enrollments/{enrollmentId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Learners.DetailView)]
    public Task<LearnerDto> GetEnrollment(Guid enrollmentId, CancellationToken ct) => mediator.Send(new GetEnrollmentLearnerQuery(new EnrollmentId(enrollmentId)), ct);
    [HttpPatch("enrollments/{enrollmentId:guid}/status"), HasPermission(PedagoraPilotPermissionCodes.Learners.Update)]
    public Task<LearnerDto> ChangeStatus(Guid enrollmentId, ChangeEnrollmentStatusRequest request, CancellationToken ct) => mediator.Send(new ChangeEnrollmentStatusCommand(new EnrollmentId(enrollmentId), request.Status, request.EndedOn), ct);
}
