using System.Text.Json;
namespace PedagoraPilot.Contracts.Branding;
public sealed record BrandingRevisionDto(Guid Id, Guid OrganizationId, JsonElement Configuration, DateTime CreatedAtUtc);
public sealed record BrandingStateDto(Guid OrganizationId, Guid? DraftRevisionId, Guid? PublishedRevisionId, BrandingRevisionDto? Draft, BrandingRevisionDto? Published);
public sealed record SaveBrandingDraftRequest(JsonElement Configuration);
public sealed record BrandingAssetDto(Guid Id, string ContentType, long SizeBytes);
