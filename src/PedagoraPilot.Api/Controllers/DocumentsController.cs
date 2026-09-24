using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Documents;
using PedagoraPilot.Contracts.Documents;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.View)]
    public Task<IReadOnlyCollection<DocumentDto>> Get([FromQuery] Guid? cohortId, [FromQuery] string? category, [FromQuery] string? ownerType, [FromQuery] Guid? ownerId, CancellationToken ct) => mediator.Send(new GetDocumentsQuery(cohortId, category, ownerType, ownerId), ct);
    [HttpGet("{id:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.View)]
    public Task<DocumentDto> GetOne(Guid id, CancellationToken ct) => mediator.Send(new GetDocumentQuery(new DocumentId(id)), ct);
    [HttpGet("{id:guid}/content")]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.View)]
    public async Task<IActionResult> Download(Guid id, [FromQuery] Guid? versionId, CancellationToken ct)
    {
        var file = await mediator.Send(new DownloadDocumentQuery(new DocumentId(id), versionId.HasValue ? new DocumentVersionId(versionId.Value) : null), ct);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.Manage)]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentForm form, CancellationToken ct)
    {
        await using var stream = form.File.OpenReadStream();
        var result = await mediator.Send(new UploadDocumentCommand(form.Title, form.Description, form.Category, form.Visibility, form.OwnerType, form.OwnerId, form.SiteId, form.ProgramId, form.CohortId, form.AuthorDisplayName, form.File.FileName, form.File.ContentType, form.File.Length, stream), ct);
        return Created($"/api/v1/documents/{result.Id}", result);
    }

    [HttpPost("{id:guid}/versions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.Manage)]
    public async Task<DocumentDto> Replace(Guid id, [FromForm] ReplaceDocumentVersionForm form, CancellationToken ct)
    {
        await using var stream = form.File.OpenReadStream();
        return await mediator.Send(new ReplaceDocumentVersionCommand(new DocumentId(id), form.AuthorDisplayName, form.File.FileName, form.File.ContentType, form.File.Length, stream), ct);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.Manage)]
    public Task<DocumentDto> Update(Guid id, UpdateDocumentMetadataRequest request, CancellationToken ct) => mediator.Send(new UpdateDocumentMetadataCommand(new DocumentId(id), request.Title, request.Description, request.Category, request.Visibility), ct);
    [HttpDelete("{id:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.Documents.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteDocumentCommand(new DocumentId(id)), ct);
        return NoContent();
    }
}

public sealed class UploadDocumentForm
{
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string Category { get; init; } = "other";
    public string Visibility { get; init; } = "staff";
    public string OwnerType { get; init; } = "none";
    public Guid? OwnerId { get; init; }
    public Guid? SiteId { get; init; }
    public Guid? ProgramId { get; init; }
    public Guid? CohortId { get; init; }
    public string AuthorDisplayName { get; init; } = "Utilisateur";
    public IFormFile File { get; init; } = default!;
}

public sealed class ReplaceDocumentVersionForm
{
    public string AuthorDisplayName { get; init; } = "Utilisateur";
    public IFormFile File { get; init; } = default!;
}
