using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class DistanceLearningSessionRepository(PedagoraPilotDbContext db) : Repository<DistanceLearningSession, DistanceLearningSessionId>(db), IDistanceLearningSessionRepository;
public sealed class AsyncLearningModuleRepository(PedagoraPilotDbContext db) : Repository<AsyncLearningModule, AsyncLearningModuleId>(db), IAsyncLearningModuleRepository;
