using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Training.Delivery;
public sealed class TrainingSessionParticipant : Entity<TrainingSessionParticipantId>
{
    private TrainingSessionParticipant()
    {
    }

    internal TrainingSessionParticipant(TrainingSessionParticipantId id, EnrollmentId enrollmentId) : base(id)
    {
        if (enrollmentId.IsEmpty)
            throw new DomainException("SESSION_PARTICIPANT_ENROLLMENT_REQUIRED");
        EnrollmentId = enrollmentId;
    }

    public EnrollmentId EnrollmentId { get; private set; }
}
