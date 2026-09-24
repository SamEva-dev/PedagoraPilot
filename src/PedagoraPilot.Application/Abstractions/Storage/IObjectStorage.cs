namespace PedagoraPilot.Application.Abstractions.Storage;
public sealed record ObjectStorageWriteResult(string StorageKey, long SizeBytes);
public interface IObjectStorage
{
    Task<ObjectStorageWriteResult> PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
