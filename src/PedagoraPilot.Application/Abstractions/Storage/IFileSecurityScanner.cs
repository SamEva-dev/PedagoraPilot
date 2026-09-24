namespace PedagoraPilot.Application.Abstractions.Storage;
public enum FileSecurityScanResult
{
    Clean = 0,
    Rejected = 1
}

public interface IFileSecurityScanner
{
    Task<FileSecurityScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}
