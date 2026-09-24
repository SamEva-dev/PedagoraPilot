using Microsoft.EntityFrameworkCore;

namespace PedagoraPilot.Infrastructure.Persistence.Catalog;
[PrimaryKey(nameof(ProgramId), nameof(CapabilityCode))]
public sealed class ProgramCapabilityRow
{
    public Guid ProgramId { get; set; }
    public string CapabilityCode { get; set; } = string.Empty;
}

[PrimaryKey(nameof(ReferentialVersionId), nameof(CapabilityCode))]
public sealed class ReferentialVersionCapabilityRow
{
    public Guid ReferentialVersionId { get; set; }
    public string CapabilityCode { get; set; } = string.Empty;
}
