using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Application.Signatures;

/// <summary>
/// The caller must already be authorized for the document's tenant and training cohort.
/// A colleague sees signed records; only an authorized supervisor also sees refusals.
/// Drafts/pending records remain private to the signer and the author.
/// </summary>
public static class SignatureVisibilityPolicy
{
    public static bool CanReadContent(bool isSigner, bool isAuthor, bool isSupervisor, SignatureEnvelopeStatus status)
        => isSigner || isAuthor || status == SignatureEnvelopeStatus.Signed
            || (isSupervisor && status == SignatureEnvelopeStatus.Declined);

    public static bool CanListInLearnerHistory(bool isSigner, bool isAuthor, bool assignedTrainer,
        bool isSupervisor, SignatureEnvelopeStatus status)
        => isSigner && status != SignatureEnvelopeStatus.Draft
            || isAuthor
            || status == SignatureEnvelopeStatus.Signed && (assignedTrainer || isSupervisor)
            || status == SignatureEnvelopeStatus.Declined && isSupervisor;
}
