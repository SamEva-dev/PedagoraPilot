using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using PedagoraPilot.Application.Abstractions.Storage;

namespace PedagoraPilot.Infrastructure.Storage;

/// <summary>Streams the full file through clamd INSTREAM. Rejects malware and
/// fails closed on all socket/protocol errors. Never interprets a timeout as clean.</summary>
public sealed class ClamAvFileSecurityScanner(IOptions<ClamAvScannerOptions> options) : IFileSecurityScanner, IProductionConnectivityProbe
{
    private readonly ClamAvScannerOptions _options = options.Value;

    public async Task<FileSecurityScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!content.CanRead) throw new ArgumentException("A readable stream is required.", nameof(content));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, timeout.Token);
        await using var socket = client.GetStream();
        await socket.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
        var buffer = new byte[64 * 1024];
        var lengthBuffer = new byte[4];
        int read;
        while ((read = await content.ReadAsync(buffer, timeout.Token)) != 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(lengthBuffer, (uint)read);
            await socket.WriteAsync(lengthBuffer, timeout.Token);
            await socket.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
        }
        BinaryPrimitives.WriteUInt32BigEndian(lengthBuffer, 0);
        await socket.WriteAsync(lengthBuffer, timeout.Token);
        await socket.FlushAsync(timeout.Token);
        var response = await ReadResponseAsync(socket, timeout.Token);
        if (response.EndsWith(" FOUND", StringComparison.Ordinal)) return FileSecurityScanResult.Rejected;
        if (response.Equals("stream: OK", StringComparison.Ordinal)) return FileSecurityScanResult.Clean;
        throw new IOException("Document scanner returned an unexpected response; upload refused.");
    }

    public async Task VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, timeout.Token);
        await using var socket = client.GetStream();
        await socket.WriteAsync("zPING\0"u8.ToArray(), timeout.Token);
        var response = await ReadResponseAsync(socket, timeout.Token);
        if (!string.Equals(response, "PONG", StringComparison.Ordinal))
            throw new IOException("Document scanner health check failed.");
    }

    private static async Task<string> ReadResponseAsync(NetworkStream socket, CancellationToken ct)
    {
        var reply = new byte[4096];
        var length = 0;
        while (length < reply.Length)
        {
            var received = await socket.ReadAsync(reply.AsMemory(length, 1), ct);
            if (received == 0) break;
            if (reply[length] == 0 || reply[length] == (byte)'\n')
                return Encoding.UTF8.GetString(reply, 0, length).Trim();
            length += received;
        }
        throw new IOException("Document scanner closed the connection or returned an oversized response.");
    }
}
