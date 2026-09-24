using DomainRelay.Abstractions;

namespace PedagoraPilot.Application.Abstractions.Messaging;
public interface ITransactionalRequest;
public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactionalRequest;
public interface IQuery<out TResponse> : IRequest<TResponse>;
