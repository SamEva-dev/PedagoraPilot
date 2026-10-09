using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PedagoraPilot.Application.Abstractions.Storage;

namespace PedagoraPilot.Infrastructure.Storage;

/// <summary>Private S3/MinIO storage; no public URLs or unauthenticated downloads.
/// Credentials are resolved by the AWS SDK's standard provider chain.</summary>
public sealed class S3ObjectStorage(IAmazonS3 client, IOptions<S3StorageOptions> options) : IObjectStorage, IProductionConnectivityProbe
{
    private readonly string _bucket = options.Value.Bucket;
    private readonly string _prefix = NormalizePrefix(options.Value.KeyPrefix);

    public async Task<ObjectStorageWriteResult> PutAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (!content.CanRead || !content.CanSeek)
            throw new ArgumentException("S3 upload requires a seekable, readable stream.", nameof(content));
        var size = content.Length - content.Position;
        if (size < 0) throw new ArgumentException("Invalid upload stream position.", nameof(content));
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = Key(storageKey),
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await client.PutObjectAsync(request, cancellationToken);
        return new ObjectStorageWriteResult(storageKey, size);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = Key(storageKey) }, cancellationToken);
        // Return an owning stream: closing the caller's stream also frees S3 HTTP resources.
        return new OwnedS3ResponseStream(response);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = _bucket, Key = Key(storageKey) }, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
        client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = Key(storageKey) }, cancellationToken);

    public async Task VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = _bucket }, cancellationToken);
    }

    private string Key(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.", nameof(storageKey));
        var segments = storageKey.Replace('\\', '/').Split('/');
        if (segments.Any(x => x.Length == 0 || x is "." or ".."))
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        return _prefix + string.Join('/', segments);
    }

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return "";
        if (prefix.Trim('/').Length == 0) return "";
        var parts = prefix.Trim('/').Split('/');
        if (parts.Any(x => x.Length == 0 || x is "." or ".."))
            throw new ArgumentException("Invalid S3 key prefix.", nameof(prefix));
        return string.Join('/', parts) + '/';
    }

    private sealed class OwnedS3ResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _stream = response.ResponseStream;
        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }
        public override void Flush() => _stream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _stream.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) response.Dispose(); base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { response.Dispose(); await base.DisposeAsync(); }
    }
}
