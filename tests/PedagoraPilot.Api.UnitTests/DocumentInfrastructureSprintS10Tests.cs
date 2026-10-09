using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Infrastructure.Storage;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class DocumentInfrastructureSprintS10Tests
{
    [Theory]
    [InlineData("stream: OK\0", FileSecurityScanResult.Clean)]
    [InlineData("stream: Eicar-Test-Signature FOUND\0", FileSecurityScanResult.Rejected)]
    public async Task Clamav_instream_maps_verified_responses(string response, FileSecurityScanResult expected)
    {
        var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        var port = ((IPEndPoint)server.LocalEndpoint).Port;
        using var serverCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var receiver = Task.Run(async () =>
        {
            using var client = await server.AcceptTcpClientAsync(serverCts.Token);
            await using var wire = client.GetStream();
            var command = new byte[10];
            await wire.ReadExactlyAsync(command, serverCts.Token);
            Encoding.ASCII.GetString(command).Should().Be("zINSTREAM\0");
            var received = new List<byte>();
            var count = new byte[4];
            while (true)
            {
                await wire.ReadExactlyAsync(count, serverCts.Token);
                var size = BinaryPrimitives.ReadUInt32BigEndian(count);
                if (size == 0) break;
                size.Should().BeLessThanOrEqualTo(64u * 1024u);
                var payload = new byte[(int)size];
                await wire.ReadExactlyAsync(payload, serverCts.Token);
                received.AddRange(payload);
            }
            Encoding.UTF8.GetString(received.ToArray()).Should().Be("document-test");
            await wire.WriteAsync(Encoding.UTF8.GetBytes(response), serverCts.Token);
        });
        var scanner = new ClamAvFileSecurityScanner(Options.Create(new ClamAvScannerOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 3 }));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("document-test"));
        (await scanner.ScanAsync(stream, "sample.pdf", "application/pdf", serverCts.Token)).Should().Be(expected);
        await receiver;
    }

    [Fact]
    public async Task Scanner_fails_closed_when_server_returns_invalid_protocol()
    {
        var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        var port = ((IPEndPoint)server.LocalEndpoint).Port;
        using var token = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var receiver = Task.Run(async () =>
        {
            using var client = await server.AcceptTcpClientAsync(token.Token);
            await using var wire = client.GetStream();
            var prefix = new byte[10];
            await wire.ReadExactlyAsync(prefix, token.Token);
            var length = new byte[4];
            while (true)
            {
                await wire.ReadExactlyAsync(length, token.Token);
                var n = (int)BinaryPrimitives.ReadUInt32BigEndian(length);
                if (n == 0) break;
                var data = new byte[n];
                await wire.ReadExactlyAsync(data, token.Token);
            }
            await wire.WriteAsync("stream: ERROR\0"u8.ToArray(), token.Token);
        });
        var scanner = new ClamAvFileSecurityScanner(Options.Create(new ClamAvScannerOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 3 }));
        await using var content = new MemoryStream("x"u8.ToArray());
        Func<Task> act = () => scanner.ScanAsync(content, "sample.pdf", "application/pdf", token.Token);
        await act.Should().ThrowAsync<IOException>();
        await receiver;
    }

    [Fact]
    public async Task Scanner_rejects_unreachable_service_instead_of_accepting_file()
    {
        // Reserve and close a random ephemeral port to avoid any reliance on external services.
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var scanner = new ClamAvFileSecurityScanner(Options.Create(new ClamAvScannerOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 2 }));
        await using var content = new MemoryStream("x"u8.ToArray());
        Func<Task> act = () => scanner.ScanAsync(content, "sample.pdf", "application/pdf");
        // Depending on the OS, connection refusal can arrive after the scanner timeout.
        var failure = await act.Should().ThrowAsync<Exception>();
        failure.Which.Should().Match<Exception>(ex => ex is SocketException || ex is OperationCanceledException);
    }
}
