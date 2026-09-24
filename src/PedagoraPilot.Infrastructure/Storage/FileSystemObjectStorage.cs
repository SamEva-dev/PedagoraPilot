using Microsoft.Extensions.Options;
using PedagoraPilot.Application.Abstractions.Storage;

namespace PedagoraPilot.Infrastructure.Storage;
public sealed class FileSystemObjectStorage : IObjectStorage
{
    private readonly string _root;
    public FileSystemObjectStorage(IOptions<ObjectStorageOptions> options)
    {
        var configured = options.Value.RootPath;
        var home = Environment.GetEnvironmentVariable("PEDAGORA_PILOT_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PedagoraPilot");
        _root = configured.Replace("%PEDAGORA_PILOT_HOME%", home, StringComparison.OrdinalIgnoreCase);
        _root = Path.GetFullPath(_root);
        Directory.CreateDirectory(_root);
    }

    public async Task<ObjectStorageWriteResult> PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(target, cancellationToken);
        await target.FlushAsync(cancellationToken);
        return new ObjectStorageWriteResult(storageKey, target.Length);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        Stream stream = new FileStream(Resolve(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult(File.Exists(Resolve(storageKey)));
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        var path = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage key.");
        return path;
    }
}
