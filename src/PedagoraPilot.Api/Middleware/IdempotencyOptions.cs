namespace PedagoraPilot.Api.Middleware;
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";
    public bool Enabled { get; set; } = true;
    public bool RequireForUnsafeMethods { get; set; } = true;
    public int RetentionHours { get; set; } = 24;
    public int MaxResponseBytes { get; set; } = 1_048_576;
}
