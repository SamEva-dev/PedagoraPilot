using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Signatures;

/// <summary>Explicit authenticated view acknowledgement; GET queries remain read-only.</summary>
public sealed record MarkSignatureViewedCommand(SignatureEnvelopeId Id, bool Attendance)
    : ICommand<SignatureEnvelopeDto>;
