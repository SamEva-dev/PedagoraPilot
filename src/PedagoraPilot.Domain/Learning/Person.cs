using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning.Events;

namespace PedagoraPilot.Domain.Learning;
public sealed class Person : AggregateRoot<PersonId>
{
    private Person()
    {
    }

    private Person(PersonId id, string firstName, string lastName, string email, string? phone, DateOnly? birthDate, string? externalKey) : base(id)
    {
        FirstName = Required(firstName, "PERSON_FIRST_NAME_REQUIRED", 100);
        LastName = Required(lastName, "PERSON_LAST_NAME_REQUIRED", 100);
        Email = NormalizeEmail(email);
        Phone = Optional(phone, 40);
        BirthDate = birthDate;
        ExternalKey = Optional(externalKey, 100);
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public static Person Create(string firstName, string lastName, string email, string? phone, DateOnly? birthDate, string? externalKey = null)
    {
        var person = new Person(PersonId.New(), firstName, lastName, email, phone, birthDate, externalKey);
        person.RaiseDomainEvent(new PersonCreatedDomainEvent(person.Id));
        return person;
    }

    public void UpdateContact(string firstName, string lastName, string email, string? phone, DateOnly? birthDate)
    {
        FirstName = Required(firstName, "PERSON_FIRST_NAME_REQUIRED", 100);
        LastName = Required(lastName, "PERSON_LAST_NAME_REQUIRED", 100);
        Email = NormalizeEmail(email);
        Phone = Optional(phone, 40);
        BirthDate = birthDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeEmail(string value)
    {
        var email = Required(value, "PERSON_EMAIL_REQUIRED", 256).ToLowerInvariant();
        if (!email.Contains('@', StringComparison.Ordinal))
            throw new DomainException("PERSON_EMAIL_INVALID");
        return email;
    }

    private static string Required(string value, string errorKey, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(errorKey);
        var result = value.Trim();
        if (result.Length > max)
            throw new DomainException(errorKey);
        return result;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var result = value.Trim();
        return result.Length <= max ? result : result[..max];
    }
}
