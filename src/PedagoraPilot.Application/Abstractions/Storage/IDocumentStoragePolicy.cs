namespace PedagoraPilot.Application.Abstractions.Storage;
public interface IDocumentStoragePolicy
{
    long MaxFileSizeBytes { get; }

    bool IsAllowed(string fileName, string contentType);
}
