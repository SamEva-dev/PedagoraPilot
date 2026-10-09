using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Access.Invitations;
using PedagoraPilot.Application.Training.Cohorts;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/cohorts"), Authorize]
public sealed class CohortsController(IMediator mediator) : ControllerBase
{
    [HttpGet, HasPermission(PedagoraPilotPermissionCodes.Cohorts.View)]
    public Task<IReadOnlyCollection<CohortDto>> Get([FromQuery] Guid? organizationId, [FromQuery] Guid? siteId, [FromQuery] Guid? programId, CancellationToken ct) => mediator.Send(new GetCohortsQuery(organizationId, siteId, programId), ct);
    [HttpPost, HasPermission(PedagoraPilotPermissionCodes.Cohorts.Manage)]
    public async Task<ActionResult<CohortDto>> Create(CreateCohortRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCohortCommand(request.ProgramOfferingId, request.ReferentialVersionId, request.Code, request.Name, request.StartDate, request.EndDate, request.Capacity, request.PedagogicalManagerAuthGateUserId, request.PedagogicalManagerDisplayName, request.ExternalKey, request.InitialStatus), ct);
        return Created($"/api/v1/cohorts/{result.Id}", result);
    }

    [HttpPut("{id:guid}/pedagogical-manager"), HasPermission(PedagoraPilotPermissionCodes.Cohorts.Manage)]
    public Task<CohortDto> ReassignPedagogicalManager(Guid id, ReassignCohortManagerRequest request, CancellationToken ct)
        => mediator.Send(new ReassignCohortManagerCommand(new CohortId(id), request.AuthGateUserId), ct);

    [HttpDelete("{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Cohorts.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCohortCommand(new CohortId(id)), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Cohorts.Manage)]
    public Task<CohortDto> Update(Guid id, UpdateCohortRequest request, CancellationToken ct) => mediator.Send(new UpdateCohortCommand(new CohortId(id), request.Name, request.StartDate, request.EndDate, request.Capacity, request.Status), ct);
    public sealed record InviteCohortLearnerRequest(string FirstName, string LastName, string Email,
        string? Phone, DateOnly? BirthDate, DateOnly? EnrolledOn);

    /// <summary>
    /// Queues a learner invitation scoped to this cohort. A LearnerProfile/Enrollment is NOT
    /// created until the invitee accepts and the identity/member provisioning succeeds.
    /// </summary>
    [HttpPost("{id:guid}/invitations"), HasPermission(PedagoraPilotPermissionCodes.Learners.Create)]
    public async Task<IActionResult> InviteLearner(Guid id, [FromBody] InviteCohortLearnerRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateAccessInvitationCommand(
            request.FirstName, request.LastName, request.Email, "student",
            CohortId: id, Phone: request.Phone, BirthDate: request.BirthDate, EnrolledOn: request.EnrolledOn), ct);
        return Accepted(new { id = result.Id, result.Email, result.FirstName, result.LastName,
            role = result.Role, status = result.Status });
    }

    [HttpGet("{id:guid}/learners"), HasPermission(PedagoraPilotPermissionCodes.Learners.View)]
    public Task<IReadOnlyCollection<LearnerDto>> Learners(Guid id, CancellationToken ct) => mediator.Send(new GetCohortLearnersQuery(new CohortId(id)), ct);
    [HttpPost("{id:guid}/learners"), HasPermission(PedagoraPilotPermissionCodes.Learners.Create)]
    public async Task<ActionResult<LearnerDto>> Enroll(Guid id, EnrollLearnerRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new EnrollLearnerCommand(new CohortId(id), request.FirstName, request.LastName, request.Email, request.Phone, request.BirthDate, request.EnrolledOn, request.AuthGateUserId, request.PersonExternalKey, request.LearnerExternalKey, request.EnrollmentExternalKey), ct);
        return Created($"/api/v1/learners/{result.LearnerProfileId}", result);
    }
}
