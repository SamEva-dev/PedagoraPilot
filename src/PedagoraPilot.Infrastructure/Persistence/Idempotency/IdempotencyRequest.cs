namespace PedagoraPilot.Infrastructure.Persistence.Idempotency;
public sealed class IdempotencyRequest
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public int? ResponseStatusCode { get; set; }
    public string? ResponseContentType { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
