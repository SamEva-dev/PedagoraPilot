using PedagoraPilot.Application.Abstractions.Persistence;

namespace PedagoraPilot.Application.Common.Behaviors;

public sealed class TransactionCompensation : ITransactionCompensation
{
    private readonly Stack<Func<CancellationToken, Task>> _compensations = new();

    public void Register(Func<CancellationToken, Task> compensation)
    {
        ArgumentNullException.ThrowIfNull(compensation);
        _compensations.Push(compensation);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        List<Exception>? failures = null;

        while (_compensations.TryPop(out var compensation))
        {
            try
            {
                await compensation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures ??= [];
                failures.Add(exception);
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("One or more transaction compensations failed.", failures);
    }

    public void Clear() => _compensations.Clear();
}
