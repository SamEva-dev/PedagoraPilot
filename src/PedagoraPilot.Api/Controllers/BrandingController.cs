using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Application.Branding;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Contracts.Branding;
using PedagoraPilot.Domain.Branding;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Security.Contracts;
namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/organizations/{organizationId:guid}/branding")]
[Authorize]
public sealed class BrandingController(BrandingService branding,IBrandingRepository repository,IUnitOfWork uow,IObjectStorage storage) : ControllerBase
{
    [HttpGet,HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public Task<BrandingStateDto> Get(Guid organizationId,CancellationToken ct)=>branding.GetAsync(organizationId,ct);
    [HttpPut("draft"),HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public Task<BrandingStateDto> Draft(Guid organizationId,[FromBody]SaveBrandingDraftRequest request,CancellationToken ct)=>branding.SaveDraftAsync(organizationId,request,ct);
    [HttpPost("publish/{revisionId:guid}"),HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public Task<BrandingStateDto> Publish(Guid organizationId,Guid revisionId,CancellationToken ct)=>branding.PublishAsync(organizationId,revisionId,ct);
    [HttpPost("restore/{revisionId:guid}"),HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public Task<BrandingStateDto> Restore(Guid organizationId,Guid revisionId,CancellationToken ct)=>branding.RestoreAsync(organizationId,revisionId,ct);
    [HttpPost("reset-to-default"),HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public Task<BrandingStateDto> ResetToDefault(Guid organizationId,CancellationToken ct)
        =>branding.ResetToNativeThemeAsync(organizationId,ct);
    [HttpPost("assets"),HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8_388_608)]
    public async Task<ActionResult<BrandingAssetDto>> Upload(Guid organizationId,IFormFile file,CancellationToken ct)
    {
        branding.RequireAccess(organizationId);
        if(!await repository.OrganizationExistsAsync(organizationId,ct)) return NotFound();
        if(file.Length is <= 0 or > 8_388_608) return BadRequest("branding.invalid_file_size");
        // Raster-only until a dedicated SVG sanitizer is integrated.
        var signatures = new Dictionary<string,byte[]> { ["image/png"]=[137,80,78,71,13,10,26,10], ["image/jpeg"]=[255,216,255], ["image/webp"]=[82,73,70,70] };
        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if(!signatures.TryGetValue(contentType,out var signature)) return BadRequest("branding.invalid_media_type");
        // The upload stream is not guaranteed to be seekable; buffer only after the size limit.
        await using var input=file.OpenReadStream();
        await using var stream=new MemoryStream();
        await input.CopyToAsync(stream,ct);
        if (stream.Length != file.Length || stream.Length > 8_388_608) return BadRequest("branding.invalid_file_size");
        if (stream.Length < 12) return BadRequest("branding.invalid_file_signature");
        var header=stream.ToArray().AsSpan(0,12);
        if (!header.StartsWith(signature) || (contentType == "image/webp" && !header.Slice(8,4).SequenceEqual("WEBP"u8)))
            return BadRequest("branding.invalid_file_signature");
        stream.Position=0;
        var assetId=Guid.NewGuid();var key=$"branding/{organizationId:D}/{assetId:D}";
        await storage.PutAsync(key,stream,contentType,ct);
        try
        {
            var asset=BrandingAsset.Create(organizationId,key,contentType,file.Length);repository.Add(asset);await uow.SaveChangesAsync(ct);
            return Ok(new BrandingAssetDto(asset.Id,asset.ContentType,asset.SizeBytes));
        }
        catch { await storage.DeleteAsync(key,ct);throw; }
    }
    [HttpGet("assets/{assetId:guid}/preview")]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public async Task<IActionResult> PreviewAsset(Guid organizationId,Guid assetId,CancellationToken ct)
    {
        branding.RequireAccess(organizationId);
        var asset=await repository.GetAssetAsync(organizationId,assetId,ct);
        return asset is null ? NotFound() : File(await storage.OpenReadAsync(asset.StorageKey,ct),asset.ContentType,enableRangeProcessing:true);
    }
    [HttpGet("assets/{assetId:guid}/content"),AllowAnonymous]
    public async Task<IActionResult> Asset(Guid organizationId,Guid assetId,CancellationToken ct)
    {
        var asset=await repository.GetAssetAsync(organizationId,assetId,ct);
        if(asset is null) return NotFound();
        // Only published assets are public. Previews need an authenticated private route.
        if (!await branding.IsPublishedAssetAsync(organizationId,assetId,ct)) return NotFound();
        return File(await storage.OpenReadAsync(asset.StorageKey,ct),asset.ContentType,enableRangeProcessing:true);
    }
}
[ApiController]
[Route("api/v1/public/branding")]
public sealed class PublicBrandingController(BrandingService branding) : ControllerBase
{
    [HttpGet("{organizationId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid organizationId,CancellationToken ct)
    {
        var published=await branding.PublicAsync(organizationId,ct);
        return published is null ? NotFound() : Ok(published);
    }
}
