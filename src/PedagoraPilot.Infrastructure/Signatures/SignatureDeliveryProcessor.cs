using System.Net;
using Itech.Emailing.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Signatures;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Infrastructure.Signatures;

/// <summary>Runs after transaction commit through DomainRelay's outbox. Never exposes snapshot data by email.</summary>
public sealed class SignatureDeliveryProcessor(ISignatureEnvelopeRepository envelopes,
    IEnrollmentRepository enrollments, ILearnerProfileRepository profiles, IPersonRepository persons,
    IIdempotentEmailingService emailing, IConfiguration config, IHostEnvironment environment) : ISignatureDeliveryProcessor
{
    public Task DeliverRequestAsync(Guid envelopeId, Guid eventId, CancellationToken ct) => DeliverAsync(envelopeId, eventId, false, ct);
    public Task DeliverReminderAsync(Guid envelopeId, Guid eventId, CancellationToken ct) => DeliverAsync(envelopeId, eventId, true, ct);

    private async Task DeliverAsync(Guid envelopeId, Guid eventId, bool reminder, CancellationToken ct)
    {
        var envelope = await envelopes.GetByIdAsync(new SignatureEnvelopeId(envelopeId), false, ct)
            ?? throw new InvalidOperationException("SIGNATURE_NOT_FOUND");
        if (envelope.SourceType is not (SignatureSourceType.PedagogicalObservation or SignatureSourceType.Attendance or SignatureSourceType.DailyObservationSummary)
            || envelope.Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed)) return;
        if (envelope.ExpiresAtUtc <= DateTimeOffset.UtcNow) return;
        if (reminder && (envelope.ReminderCount < 1 || !envelope.LastReminderAtUtc.HasValue)) return;
        var enrollment = await enrollments.GetByIdAsync(envelope.EnrollmentId, false, ct)
            ?? throw new InvalidOperationException("SIGNATURE_ENROLLMENT_NOT_FOUND");
        if (enrollment.OrganizationId != envelope.OrganizationId || enrollment.CohortId.Value != envelope.CohortId)
            throw new InvalidOperationException("SIGNATURE_CONTEXT_INVALID");
        var profile = await profiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new InvalidOperationException("SIGNATURE_PROFILE_NOT_FOUND");
        if (profile.AuthGateUserId != envelope.SignerAuthGateUserId)
            throw new InvalidOperationException("SIGNATURE_SIGNER_CHANGED");
        var person = await persons.GetByIdAsync(profile.PersonId, false, ct)
            ?? throw new InvalidOperationException("SIGNATURE_PERSON_NOT_FOUND");
        var baseUrl = config["Signatures:FrontendBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && (environment.IsProduction() || !uri.IsLoopback)))
            throw new InvalidOperationException("Signatures:FrontendBaseUrl must be an HTTPS URL.");
        var isAttendance = envelope.SourceType == SignatureSourceType.Attendance;
        var url = isAttendance
            ? $"{baseUrl}/signatures/attendance/{envelope.Id.Value:D}"
            : $"{baseUrl}/signatures/{envelope.Id.Value:D}";
        var safeUrl = WebUtility.HtmlEncode(url);
        var subject = isAttendance ? "PedagoraPilot — émargement à confirmer" : "PedagoraPilot — observation à consulter";
        var description = isAttendance ? "Un relevé de présence vous est adressé." : "Une observation pédagogique vous est adressée.";
        if (reminder) { subject = "Rappel — " + subject; description = "Rappel : " + description; }
        var disclaimer = isAttendance
            ? "Cette attestation confirme votre prise de connaissance des horaires affichés. Vous pouvez contester les horaires."
            : "La prise de connaissance ne vaut pas approbation.";
        await emailing.QueueHtmlOnceAsync(person.Email, subject,
            $"<p>{WebUtility.HtmlEncode(description)}</p><p><a href=\"{safeUrl}\">Consulter et répondre</a></p><p>Connectez-vous avec votre propre compte pour y accéder. {WebUtility.HtmlEncode(disclaimer)}</p>",
            $"{description} Connectez-vous avec votre compte pour consulter : {url}. {disclaimer}",
            attachments: null, tags: EmailUseCaseTags.NotificationSystem,
            idempotencyKey: $"pedagora:signature:{envelope.OrganizationId:N}:{eventId:N}",
            tenantId: envelope.OrganizationId, cancellationToken: ct);
    }
}
