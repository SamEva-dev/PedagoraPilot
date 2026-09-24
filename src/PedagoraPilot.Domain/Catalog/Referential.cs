using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog;
public sealed class Referential : AggregateRoot
{
    private Referential()
    {
    }

    private Referential(Guid id, Guid programId, string code, string name, string? externalKey) : base(id)
    {
        ProgramId = programId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ExternalKey = externalKey;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid ProgramId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static Referential Create(Guid programId, string code, string name, string? externalKey = null) => new(Guid.NewGuid(), programId, code, name, externalKey);
}
