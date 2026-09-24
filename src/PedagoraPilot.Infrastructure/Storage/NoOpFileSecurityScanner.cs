using PedagoraPilot.Application.Abstractions.Storage;

namespace PedagoraPilot.Infrastructure.Storage;
/// <summary>
/// Development/default scanner. The abstraction is intentionally present from BE-07 so a
/// ClamAV/ICAP scanner can replace this implementation without changing application code.
/// Production environments should replace this registration.
/// </summary>
public sealed class NoOpFileSecurityScanner : IFileSecurityScanner
{
    public Task<FileSecurityScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default) => Task.FromResult(FileSecurityScanResult.Clean);
}
