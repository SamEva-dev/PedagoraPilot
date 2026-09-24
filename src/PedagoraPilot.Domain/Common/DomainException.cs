namespace PedagoraPilot.Domain.Common;
public sealed class DomainException : Exception
{
    public DomainException(string errorKey) : base(errorKey)
    {
        ErrorKey = errorKey;
    }

    public string ErrorKey { get; }
}
