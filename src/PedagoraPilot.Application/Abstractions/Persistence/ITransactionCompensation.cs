namespace PedagoraPilot.Application.Abstractions.Persistence;

/// <summary>
/// Registers compensating actions for side effects that live outside the database transaction
/// (for example object-storage writes). Actions are executed in reverse order if the unit of work fails.
/// </summary>
public interface ITransactionCompensation
{
    void Register(Func<CancellationToken, Task> compensation);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    void Clear();
}
