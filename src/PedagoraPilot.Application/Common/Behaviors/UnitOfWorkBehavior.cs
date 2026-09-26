using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;

namespace PedagoraPilot.Application.Common.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionCompensation _compensation;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork, ITransactionCompensation compensation)
    {
        _unitOfWork = unitOfWork;
        _compensation = compensation;
    }

    public Task<TResponse> Handle(TRequest request, HandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
            return next();

        return ExecuteTransactionalAsync(next, cancellationToken);
    }

    private async Task<TResponse> ExecuteTransactionalAsync(HandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _compensation.Clear();

        try
        {
            var response = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var result = await next().ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return result;
            }, cancellationToken).ConfigureAwait(false);

            _compensation.Clear();
            return response;
        }
        catch (Exception transactionException)
        {
            try
            {
                // Cleanup should still be attempted when the request token was cancelled.
                await _compensation.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception compensationException)
            {
                throw new AggregateException(
                    "The transaction failed and one or more compensating actions also failed.",
                    transactionException,
                    compensationException);
            }

            throw;
        }
    }
}
