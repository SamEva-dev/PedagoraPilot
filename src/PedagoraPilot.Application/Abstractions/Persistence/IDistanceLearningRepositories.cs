using PedagoraPilot.Domain.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IDistanceLearningSessionRepository : IRepository<DistanceLearningSession, DistanceLearningSessionId>
{
}

public interface IAsyncLearningModuleRepository : IRepository<AsyncLearningModule, AsyncLearningModuleId>
{
}
