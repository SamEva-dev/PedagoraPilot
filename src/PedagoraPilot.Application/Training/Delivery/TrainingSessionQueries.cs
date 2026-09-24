using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Training.Delivery;
public sealed record GetTrainingSessionsQuery(CohortId? CohortId = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, string? Type = null, string? Status = null) : IQuery<IReadOnlyCollection<TrainingSessionDto>>;
public sealed record GetTrainingSessionQuery(TrainingSessionId Id) : IQuery<TrainingSessionDto>;
public sealed record GetAttendanceSheetQuery(TrainingSessionId SessionId) : IQuery<AttendanceSheetDto>;
