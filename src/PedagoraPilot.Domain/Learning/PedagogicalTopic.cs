using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Learning;
public sealed class PedagogicalTopic : AggregateRoot<PedagogicalTopicId>
{
    private PedagogicalTopic()
    {
    }

    private PedagogicalTopic(PedagogicalTopicId id, Guid referentialVersionId, string code, int? number, string title, string category, int durationMinutes, string? reference, bool active, string? externalKey) : base(id)
    {
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("TOPIC_REFERENTIAL_VERSION_REQUIRED");
        if (durationMinutes <= 0 || durationMinutes > 1440)
            throw new DomainException("TOPIC_DURATION_INVALID");
        ReferentialVersionId = referentialVersionId;
        Code = Required(code, "TOPIC_CODE_REQUIRED", 80).ToUpperInvariant();
        Number = number;
        Title = Required(title, "TOPIC_TITLE_REQUIRED", 300);
        Category = Required(category, "TOPIC_CATEGORY_REQUIRED", 80);
        DurationMinutes = durationMinutes;
        Reference = Optional(reference, 500);
        Active = active;
        ExternalKey = Optional(externalKey, 120);
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid ReferentialVersionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public int? Number { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; }
    public string? Reference { get; private set; }
    public bool Active { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static PedagogicalTopic Create(Guid referentialVersionId, string code, int? number, string title, string category, int durationMinutes, string? reference = null, bool active = true, string? externalKey = null) => new(PedagogicalTopicId.New(), referentialVersionId, code, number, title, category, durationMinutes, reference, active, externalKey);
    private static string Required(string value, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var v = value.Trim();
        if (v.Length > max)
            throw new DomainException(key);
        return v;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}
