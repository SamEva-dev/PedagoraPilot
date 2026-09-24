using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog;
public sealed class ProgramFamily : AggregateRoot
{
    private ProgramFamily()
    {
    }

    private ProgramFamily(Guid id, string code, string name, string icon) : base(id)
    {
        Code = code;
        Name = name;
        Icon = icon;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Icon { get; private set; } = "ph-graduation-cap";
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static ProgramFamily Create(string code, string name, string icon) => new(Guid.NewGuid(), code.Trim().ToUpperInvariant(), name.Trim(), icon.Trim());
}
