namespace PedagoraPilot.Infrastructure.Storage;

/// <summary>Only non-secret fields belong in configuration. AWS identity/MinIO keys
/// are supplied through the standard AWS credential chain (environment, workload identity or IAM role).</summary>
public sealed class S3StorageOptions
{
    public string Bucket { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    public string ServiceUrl { get; set; } = "";
    public bool ForcePathStyle { get; set; } = true;
    public string KeyPrefix { get; set; } = "pedagora/";
}
