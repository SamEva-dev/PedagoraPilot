using System.Text.Json;
using System.Text.RegularExpressions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Contracts.Branding;
using PedagoraPilot.Domain.Branding;
using PedagoraPilot.Application.Common.Errors;
namespace PedagoraPilot.Application.Branding;
public sealed class BrandingService(IBrandingRepository repository, IUnitOfWork uow, ICurrentUser user)
{
    public void RequireAccess(Guid id)
    {
        if (!user.IsAuthenticated || (!user.IsPlatformAdministrator && user.OrganizationId != id)) throw new ForbiddenApplicationException("branding.forbidden");
    }
    public async Task<BrandingStateDto> GetAsync(Guid id, CancellationToken ct)
    {
        RequireAccess(id);
        var state=await repository.GetStateAsync(id,ct:ct);
        return await ToDto(id,state,ct);
    }
    private async Task<BrandingStateDto> ToDto(Guid id,OrganizationBranding? state,CancellationToken ct)
    {
        async Task<BrandingRevisionDto?> Load(Guid? revisionId)
        {
            if(revisionId is null) return null;
            var revision=await repository.GetRevisionAsync(id,revisionId.Value,ct);
            return revision is null ? null : new BrandingRevisionDto(revision.Id,id,JsonDocument.Parse(revision.ConfigurationJson).RootElement.Clone(),revision.CreatedAtUtc);
        }
        return new BrandingStateDto(id,state?.DraftRevisionId,state?.PublishedRevisionId,await Load(state?.DraftRevisionId),await Load(state?.PublishedRevisionId));
    }
    public async Task<BrandingStateDto> SaveDraftAsync(Guid id, SaveBrandingDraftRequest request,CancellationToken ct)
    {
        RequireAccess(id);
        if (!await repository.OrganizationExistsAsync(id,ct)) throw new NotFoundApplicationException("branding.organization_not_found");
        var json=request.Configuration.GetRawText();
        if (request.Configuration.ValueKind != JsonValueKind.Object || json.Length > 32_000)
            throw new ValidationApplicationException("branding.invalid_configuration");
        await ValidateConfigurationAsync(id, request.Configuration, ct);
        var state = await repository.GetStateAsync(id, true, ct);
        if (state is null)
        {
            state = OrganizationBranding.Create(id);
            repository.Add(state);
        }
        // A reset clears revision pointers, not the branding database row.
        // Never Add() an existing entity merely because both pointers are null.
        var revision = BrandingRevision.Create(id, json);
        repository.Add(revision);
        state.SetDraft(revision.Id);
        await uow.SaveChangesAsync(ct);
        return await ToDto(id,state,ct);
    }
    public async Task<BrandingStateDto> PublishAsync(Guid id,Guid revisionId,CancellationToken ct)
    {
        RequireAccess(id);
        var state=await repository.GetStateAsync(id,true,ct) ?? throw new NotFoundApplicationException("branding.not_found");
        if(await repository.GetRevisionAsync(id,revisionId,ct) is null) throw new NotFoundApplicationException("branding.revision_not_found");
        state.Publish(revisionId);await uow.SaveChangesAsync(ct);return await ToDto(id,state,ct);
    }
    public async Task<BrandingStateDto> RestoreAsync(Guid id,Guid revisionId,CancellationToken ct)
    {
        RequireAccess(id);
        var state=await repository.GetStateAsync(id,true,ct) ?? throw new NotFoundApplicationException("branding.not_found");
        if(await repository.GetRevisionAsync(id,revisionId,ct) is null) throw new NotFoundApplicationException("branding.revision_not_found");
        state.Restore(revisionId);await uow.SaveChangesAsync(ct);return await ToDto(id,state,ct);
    }
    public async Task<BrandingStateDto> ResetToNativeThemeAsync(Guid id, CancellationToken ct)
    {
        RequireAccess(id);
        // Preserve revisions and uploaded assets for audit / later restoration.
        var state = await repository.GetStateAsync(id, true, ct);
        if (state is null) return await ToDto(id, null, ct);
        state.ResetToNativeTheme();
        await uow.SaveChangesAsync(ct);
        return await ToDto(id, state, ct);
    }
    private async Task ValidateConfigurationAsync(Guid organizationId, JsonElement config, CancellationToken ct)
    {
        // Reject unknown properties: the public configuration is rendered before authentication.
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "name", "tagline", "primaryColor", "secondaryColor", "logoAssetId", "backgroundAssetId",
            "overlayOpacity", "formPosition", "textPosition", "backgroundPosition", "layers"
        };
        foreach (var property in config.EnumerateObject())
        {
            if (!allowed.Contains(property.Name)) throw new ValidationApplicationException("branding.invalid_configuration");
            var value = property.Value;
            switch (property.Name)
            {
                case "name": case "tagline":
                    if (value.ValueKind != JsonValueKind.String || (value.GetString()?.Length ?? 0) > (property.Name == "name" ? 120 : 240))
                        throw new ValidationApplicationException("branding.invalid_configuration");
                    break;
                case "primaryColor": case "secondaryColor":
                    if (value.ValueKind != JsonValueKind.String || !Regex.IsMatch(value.GetString() ?? "", "^#[0-9a-fA-F]{6}$"))
                        throw new ValidationApplicationException("branding.invalid_configuration");
                    break;
                case "overlayOpacity":
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var opacity) || opacity < 0 || opacity > 100)
                        throw new ValidationApplicationException("branding.invalid_configuration");
                    break;
                case "formPosition":
                    if (value.ValueKind != JsonValueKind.String || (value.GetString() is not ("left" or "right")))
                        throw new ValidationApplicationException("branding.invalid_configuration");
                    break;
                case "textPosition":
                    if (value.ValueKind != JsonValueKind.String || (value.GetString() is not ("front" or "back")))
                        throw new ValidationApplicationException("branding.invalid_configuration");
                    break;
                case "backgroundPosition":
                    if (value.ValueKind != JsonValueKind.String || !new[] { "center", "top", "bottom", "left", "right", "top left", "top right", "bottom left", "bottom right" }.Contains(value.GetString(), StringComparer.Ordinal))
                        throw new ValidationApplicationException("branding.invalid_configuration");
                    break;
                case "layers":
                    if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 60)
                        throw new ValidationApplicationException("branding.invalid_layers");
                    var uniqueIds = new HashSet<Guid>();
                    foreach (var layer in value.EnumerateArray())
                    {
                        if (layer.ValueKind != JsonValueKind.Object)
                            throw new ValidationApplicationException("branding.invalid_layers");
                        var layerProps = new HashSet<string>(StringComparer.Ordinal) { "id", "type", "assetId", "text", "x", "y", "width", "height", "zIndex", "opacity", "color", "fontSize", "groupId" };
                        foreach (var layerProp in layer.EnumerateObject())
                            if (!layerProps.Contains(layerProp.Name)) throw new ValidationApplicationException("branding.invalid_layers");
                        if (!layer.TryGetProperty("id", out var layerId) || layerId.ValueKind != JsonValueKind.String ||
                            !Guid.TryParse(layerId.GetString(), out var parsedLayerId) || !uniqueIds.Add(parsedLayerId) ||
                            !layer.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String ||
                            type.GetString() is not ("image" or "text"))
                            throw new ValidationApplicationException("branding.invalid_layers");
                        foreach (var key in new[] { "x", "y", "width", "height", "opacity", "zIndex", "fontSize" })
                        {
                            if (!layer.TryGetProperty(key, out var n) || n.ValueKind != JsonValueKind.Number || !n.TryGetDouble(out var v) ||
                                double.IsNaN(v) || double.IsInfinity(v) || v < 0 || v > (key == "zIndex" ? 1000 : key == "fontSize" ? 96 : 100) ||
                                ((key == "width" || key == "height" || key == "fontSize") && v < 1))
                                throw new ValidationApplicationException("branding.invalid_layers");
                        }
                        foreach (var key in new[] { "x", "y" })
                        {
                            var sizeKey = key == "x" ? "width" : "height";
                            if (layer.GetProperty(key).GetDouble() + layer.GetProperty(sizeKey).GetDouble() > 100.001)
                                throw new ValidationApplicationException("branding.invalid_layers");
                        }
                        if (!layer.TryGetProperty("text", out var txt) || txt.ValueKind != JsonValueKind.String || (txt.GetString()?.Length ?? 0) > 300 ||
                            !layer.TryGetProperty("color", out var col) || col.ValueKind != JsonValueKind.String || !Regex.IsMatch(col.GetString() ?? "", "^#[0-9a-fA-F]{6}$"))
                            throw new ValidationApplicationException("branding.invalid_layers");
                        if (layer.TryGetProperty("groupId", out var group) && group.ValueKind != JsonValueKind.Null &&
                            (group.ValueKind != JsonValueKind.String || !Guid.TryParse(group.GetString(), out _)))
                            throw new ValidationApplicationException("branding.invalid_layers");
                        if (type.GetString() == "image")
                        {
                            if (!layer.TryGetProperty("assetId", out var asset) || asset.ValueKind != JsonValueKind.String ||
                                !Guid.TryParse(asset.GetString(), out var imageId) || await repository.GetAssetAsync(organizationId, imageId, ct) is null)
                                throw new ValidationApplicationException("branding.invalid_asset_reference");
                        }
                        else if (layer.TryGetProperty("assetId", out var unused) && unused.ValueKind != JsonValueKind.Null)
                            throw new ValidationApplicationException("branding.invalid_layers");
                    }
                    break;
                case "logoAssetId": case "backgroundAssetId":
                    if (value.ValueKind == JsonValueKind.Null) break;
                    if (value.ValueKind != JsonValueKind.String || !Guid.TryParseExact(value.GetString(), "D", out var assetId) ||
                        await repository.GetAssetAsync(organizationId, assetId, ct) is null)
                        throw new ValidationApplicationException("branding.invalid_asset_reference");
                    break;
            }
        }
    }
    public async Task<bool> IsPublishedAssetAsync(Guid organizationId, Guid assetId, CancellationToken ct)
    {
        var published = await PublicAsync(organizationId, ct);
        if (published is null || published.Configuration.ValueKind != JsonValueKind.Object) return false;
        foreach (var name in new[] { "logoAssetId", "backgroundAssetId" })
        {
            if (published.Configuration.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var referencedId) && referencedId == assetId)
                return true;
        }
        if (published.Configuration.TryGetProperty("layers", out var layers) && layers.ValueKind == JsonValueKind.Array)
        {
            foreach (var layer in layers.EnumerateArray())
                if (layer.ValueKind == JsonValueKind.Object && layer.TryGetProperty("assetId", out var asset) &&
                    asset.ValueKind == JsonValueKind.String && Guid.TryParse(asset.GetString(), out var referencedId) && referencedId == assetId)
                    return true;
        }
        return false;
    }
    public async Task<BrandingRevisionDto?> PublicAsync(Guid organizationId,CancellationToken ct)
    {
        var state=await repository.GetStateAsync(organizationId,ct:ct);
        if(state?.PublishedRevisionId is not Guid revId) return null;
        var rev=await repository.GetRevisionAsync(organizationId,revId,ct);
        return rev is null?null:new BrandingRevisionDto(rev.Id,organizationId,JsonDocument.Parse(rev.ConfigurationJson).RootElement.Clone(),rev.CreatedAtUtc);
    }
}
