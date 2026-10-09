namespace PedagoraPilot.Infrastructure.Storage;

/// <summary>The ClamAV TCP socket MUST be isolated on a private network.
/// The INSTREAM protocol is not encrypted or authenticated.</summary>
public sealed class ClamAvScannerOptions
{
    public string Provider { get; set; } = "NoOp";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3310;
    public int TimeoutSeconds { get; set; } = 30;
    public bool ProductionRequired { get; set; } = true;
}
