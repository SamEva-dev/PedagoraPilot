using System.Security.Cryptography;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Documents;
using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Documents;
internal static class DocumentApplication
{
    public static bool TryCategory(string? value, out DocumentCategory result) => Enum.TryParse((value ?? "").Replace("-", "").Replace("_", ""), true, out result);
    public static bool TryVisibility(string? value, out DocumentVisibility result) => Enum.TryParse((value ?? "").Replace("-", "").Replace("_", ""), true, out result);
    public static bool TryOwnerType(string? value, out DocumentOwnerType result) => Enum.TryParse((value ?? "").Replace("-", "").Replace("_", ""), true, out result);
    public static DocumentDto ToDto(ManagedDocument document, IObjectMapper mapper)
    {
        var d = mapper.Map<ManagedDocument, ManagedDocumentReadModel>(document);
        return new DocumentDto(d.Id.Value, d.OrganizationId, d.SiteId, d.ProgramId, d.CohortId, d.OwnerType.ToString().ToLowerInvariant(), d.OwnerId, d.Title, d.Description, d.Category.ToString().ToLowerInvariant(), d.Visibility.ToString().ToLowerInvariant(), d.Status.ToString().ToLowerInvariant(), d.CreatedByDisplayName, d.CreatedAtUtc, d.UpdatedAtUtc, document.Versions.OrderByDescending(x => x.VersionNumber).Select(x =>
        {
            var v = mapper.Map<DocumentVersion, DocumentVersionReadModel>(x);
            return new DocumentVersionDto(v.Id.Value, v.VersionNumber, v.FileName, v.ContentType, v.SizeBytes, v.Sha256, v.BlobAvailable, v.SecurityStatus.ToString().ToLowerInvariant(), v.UploadedByDisplayName, v.UploadedAtUtc);
        }).ToArray());
    }

    public static async Task<(string TempPath, string Sha256, long SizeBytes)> BufferAndHashAsync(Stream content, long declaredSize, long maxBytes, CancellationToken ct)
    {
        if (declaredSize <= 0)
            throw new ValidationApplicationException(ErrorKeys.DocumentFileEmpty);
        if (declaredSize > maxBytes)
            throw new ValidationApplicationException(ErrorKeys.DocumentFileTooLarge);
        var path = Path.Combine(Path.GetTempPath(), $"pedagora-{Guid.NewGuid():N}.upload");
        long total = 0;
        using var sha = SHA256.Create();
        try
        {
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = new byte[81920];
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                total += read;
                if (total > maxBytes)
                    throw new ValidationApplicationException(ErrorKeys.DocumentFileTooLarge);
                sha.TransformBlock(buffer, 0, read, null, 0);
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            sha.TransformFinalBlock([], 0, 0);
            await target.FlushAsync(ct);
        }
        catch
        {
            if (File.Exists(path))
                File.Delete(path);
            throw;
        }

        if (total == 0)
        {
            File.Delete(path);
            throw new ValidationApplicationException(ErrorKeys.DocumentFileEmpty);
        }

        return (path, Convert.ToHexString(sha.Hash!).ToLowerInvariant(), total);
    }

    public static string BuildStorageKey(Guid organizationId, DocumentId documentId, int version, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return $"{organizationId:N}/{documentId.Value:N}/v{version:D4}{extension}";
    }

    public static bool IsStudent(ICurrentUser current) => current.Roles.Any(x => x.Equals("student", StringComparison.OrdinalIgnoreCase) || x.Equals("stagiaire", StringComparison.OrdinalIgnoreCase));
    public static async Task EnsureCanReadAsync(ManagedDocument document, ICurrentUser current, ILearnerProfileRepository learnerProfiles, IEnrollmentRepository enrollments, CancellationToken ct)
    {
        if (current.OrganizationId.HasValue && document.OrganizationId != current.OrganizationId.Value)
            throw new ForbiddenApplicationException(ErrorKeys.DocumentForbidden);
        if (!IsStudent(current))
            return;
        if (document.Visibility == DocumentVisibility.All)
            return;
        if (!current.UserId.HasValue || document.Visibility != DocumentVisibility.Student || document.OwnerType != DocumentOwnerType.Enrollment || !document.OwnerId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.DocumentForbidden);
        var profile = await learnerProfiles.Query(false).SingleOrDefaultAsync(x => x.AuthGateUserId == current.UserId.Value, ct);
        if (profile is null)
            throw new ForbiddenApplicationException(ErrorKeys.DocumentForbidden);
        var ownsEnrollment = await enrollments.Query(false).AnyAsync(x => x.LearnerProfileId == profile.Id && x.Id.Value == document.OwnerId.Value, ct);
        if (!ownsEnrollment)
            throw new ForbiddenApplicationException(ErrorKeys.DocumentForbidden);
    }
}

public sealed class GetDocumentsQueryHandler(IDocumentRepository documents, ILearnerProfileRepository learnerProfiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetDocumentsQuery, IReadOnlyCollection<DocumentDto>>
{
    public async Task<IReadOnlyCollection<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var query = documents.Query(false).Include(x => x.Versions).Where(x => x.Status == DocumentStatus.Active);
        if (current.OrganizationId.HasValue)
            query = query.Where(x => x.OrganizationId == current.OrganizationId.Value);
        if (request.CohortId.HasValue)
            query = query.Where(x => x.CohortId == request.CohortId.Value);
        if (!string.IsNullOrWhiteSpace(request.Category) && DocumentApplication.TryCategory(request.Category, out var category))
            query = query.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(request.OwnerType) && DocumentApplication.TryOwnerType(request.OwnerType, out var ownerType))
            query = query.Where(x => x.OwnerType == ownerType);
        if (request.OwnerId.HasValue)
            query = query.Where(x => x.OwnerId == request.OwnerId);
        if (DocumentApplication.IsStudent(current))
        {
            if (!current.UserId.HasValue)
                return[];
            var profile = await learnerProfiles.Query(false).SingleOrDefaultAsync(x => x.AuthGateUserId == current.UserId.Value, ct);
            var enrollmentIds = profile is null ? Array.Empty<Guid>() : await enrollments.Query(false).Where(x => x.LearnerProfileId == profile.Id).Select(x => x.Id.Value).ToArrayAsync(ct);
            query = query.Where(x => x.Visibility == DocumentVisibility.All || (x.Visibility == DocumentVisibility.Student && x.OwnerType == DocumentOwnerType.Enrollment && x.OwnerId.HasValue && enrollmentIds.Contains(x.OwnerId.Value)));
        }

        var rows = await query.OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(ct);
        return rows.Select(x => DocumentApplication.ToDto(x, mapper)).ToArray();
    }
}

public sealed class GetDocumentQueryHandler(IDocumentRepository documents, ILearnerProfileRepository learnerProfiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetDocumentQuery, DocumentDto>
{
    public async Task<DocumentDto> Handle(GetDocumentQuery request, CancellationToken ct)
    {
        var document = await documents.Query(false).Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == request.DocumentId && x.Status == DocumentStatus.Active, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DocumentNotFound);
        await DocumentApplication.EnsureCanReadAsync(document, current, learnerProfiles, enrollments, ct);
        return DocumentApplication.ToDto(document, mapper);
    }

    internal static void EnsureOrganization(ManagedDocument document, ICurrentUser current)
    {
        if (current.OrganizationId.HasValue && document.OrganizationId != current.OrganizationId.Value)
            throw new ForbiddenApplicationException(ErrorKeys.DocumentForbidden);
    }
}

public sealed class UploadDocumentCommandHandler(IDocumentRepository documents, IObjectStorage storage, IFileSecurityScanner securityScanner, IDocumentStoragePolicy policy, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        if (!current.OrganizationId.HasValue)
            throw new ForbiddenApplicationException(ErrorKeys.DocumentOrganizationRequired);
        if (!DocumentApplication.TryCategory(request.Category, out var category))
            throw new ValidationApplicationException(ErrorKeys.DocumentCategoryInvalid);
        if (!DocumentApplication.TryVisibility(request.Visibility, out var visibility))
            throw new ValidationApplicationException(ErrorKeys.DocumentVisibilityInvalid);
        if (!DocumentApplication.TryOwnerType(request.OwnerType, out var ownerType))
            throw new ValidationApplicationException(ErrorKeys.DocumentOwnerTypeInvalid);
        if (ownerType != DocumentOwnerType.None && !request.OwnerId.HasValue)
            throw new ValidationApplicationException(ErrorKeys.DocumentOwnerRequired);
        if (!policy.IsAllowed(request.FileName, request.ContentType))
            throw new ValidationApplicationException(ErrorKeys.DocumentFileTypeNotAllowed);
        var document = ManagedDocument.Create(current.OrganizationId.Value, request.SiteId, request.ProgramId, request.CohortId, ownerType, request.OwnerId, request.Title, request.Description, category, visibility, current.UserId?.ToString() ?? "authgate", request.AuthorDisplayName);
        var buffered = await DocumentApplication.BufferAndHashAsync(request.Content, request.SizeBytes, policy.MaxFileSizeBytes, ct);
        try
        {
            await using var scan = new FileStream(buffered.TempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var scanResult = await securityScanner.ScanAsync(scan, request.FileName, request.ContentType, ct);
            if (scanResult != FileSecurityScanResult.Clean)
                throw new ValidationApplicationException(ErrorKeys.DocumentSecurityRejected);
            var storageKey = DocumentApplication.BuildStorageKey(current.OrganizationId.Value, document.Id, 1, request.FileName);
            await using var upload = new FileStream(buffered.TempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await storage.PutAsync(storageKey, upload, request.ContentType, ct);
            document.AddVersion(request.FileName, request.ContentType, buffered.SizeBytes, buffered.Sha256, storageKey, true, DocumentSecurityStatus.Clean, current.UserId?.ToString() ?? "authgate", request.AuthorDisplayName);
            await documents.AddAsync(document, ct);
            return DocumentApplication.ToDto(document, mapper);
        }
        finally
        {
            if (File.Exists(buffered.TempPath))
                File.Delete(buffered.TempPath);
        }
    }
}

public sealed class ReplaceDocumentVersionCommandHandler(IDocumentRepository documents, IObjectStorage storage, IFileSecurityScanner securityScanner, IDocumentStoragePolicy policy, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<ReplaceDocumentVersionCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(ReplaceDocumentVersionCommand request, CancellationToken ct)
    {
        var document = await documents.Query(true).Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == request.DocumentId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DocumentNotFound);
        GetDocumentQueryHandler.EnsureOrganization(document, current);
        if (!policy.IsAllowed(request.FileName, request.ContentType))
            throw new ValidationApplicationException(ErrorKeys.DocumentFileTypeNotAllowed);
        var buffered = await DocumentApplication.BufferAndHashAsync(request.Content, request.SizeBytes, policy.MaxFileSizeBytes, ct);
        try
        {
            await using var scan = new FileStream(buffered.TempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (await securityScanner.ScanAsync(scan, request.FileName, request.ContentType, ct) != FileSecurityScanResult.Clean)
                throw new ValidationApplicationException(ErrorKeys.DocumentSecurityRejected);
            var next = document.Versions.Count == 0 ? 1 : document.Versions.Max(x => x.VersionNumber) + 1;
            var storageKey = DocumentApplication.BuildStorageKey(document.OrganizationId, document.Id, next, request.FileName);
            await using var upload = new FileStream(buffered.TempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await storage.PutAsync(storageKey, upload, request.ContentType, ct);
            document.AddVersion(request.FileName, request.ContentType, buffered.SizeBytes, buffered.Sha256, storageKey, true, DocumentSecurityStatus.Clean, current.UserId?.ToString() ?? "authgate", request.AuthorDisplayName);
            return DocumentApplication.ToDto(document, mapper);
        }
        finally
        {
            if (File.Exists(buffered.TempPath))
                File.Delete(buffered.TempPath);
        }
    }
}

public sealed class UpdateDocumentMetadataCommandHandler(IDocumentRepository documents, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<UpdateDocumentMetadataCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UpdateDocumentMetadataCommand request, CancellationToken ct)
    {
        var document = await documents.Query(true).Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == request.DocumentId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DocumentNotFound);
        GetDocumentQueryHandler.EnsureOrganization(document, current);
        if (!DocumentApplication.TryCategory(request.Category, out var category))
            throw new ValidationApplicationException(ErrorKeys.DocumentCategoryInvalid);
        if (!DocumentApplication.TryVisibility(request.Visibility, out var visibility))
            throw new ValidationApplicationException(ErrorKeys.DocumentVisibilityInvalid);
        document.UpdateMetadata(request.Title, request.Description, category, visibility);
        return DocumentApplication.ToDto(document, mapper);
    }
}

public sealed class DeleteDocumentCommandHandler(IDocumentRepository documents, ICurrentUser current) : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DocumentNotFound);
        GetDocumentQueryHandler.EnsureOrganization(document, current);
        document.Delete();
        return true;
    }
}

public sealed class DownloadDocumentQueryHandler(IDocumentRepository documents, ILearnerProfileRepository learnerProfiles, IEnrollmentRepository enrollments, IObjectStorage storage, ICurrentUser current) : IRequestHandler<DownloadDocumentQuery, DocumentDownloadDto>
{
    public async Task<DocumentDownloadDto> Handle(DownloadDocumentQuery request, CancellationToken ct)
    {
        var document = await documents.Query(false).Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == request.DocumentId && x.Status == DocumentStatus.Active, ct) ?? throw new NotFoundApplicationException(ErrorKeys.DocumentNotFound);
        await DocumentApplication.EnsureCanReadAsync(document, current, learnerProfiles, enrollments, ct);
        var version = request.VersionId.HasValue ? document.Versions.SingleOrDefault(x => x.Id == request.VersionId.Value) : document.CurrentVersion;
        if (version is null)
            throw new NotFoundApplicationException(ErrorKeys.DocumentVersionNotFound);
        if (!version.BlobAvailable || !await storage.ExistsAsync(version.StorageKey, ct))
            throw new NotFoundApplicationException(ErrorKeys.DocumentBlobNotFound);
        var stream = await storage.OpenReadAsync(version.StorageKey, ct);
        return new DocumentDownloadDto(version.FileName, version.ContentType, version.SizeBytes, stream);
    }
}
