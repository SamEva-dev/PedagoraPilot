using Microsoft.Extensions.Options;
using PedagoraPilot.Application.Abstractions.Storage;

namespace PedagoraPilot.Infrastructure.Storage;
public sealed class DocumentStoragePolicy(IOptions<ObjectStorageOptions> options) : IDocumentStoragePolicy
{
    private readonly ObjectStorageOptions _options = options.Value;
    public long MaxFileSizeBytes => _options.MaxFileSizeBytes;

    public bool IsAllowed(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        return _options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) && _options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }
}
