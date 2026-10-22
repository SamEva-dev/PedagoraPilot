using PedagoraPilot.Application.Signatures;
using System.Net;
using System.Text.Json;
using DomainRelay.Abstractions;
using DomainRelay.EFCore.Outbox;
using Itech.Emailing.Abstractions;
using PedagoraPilot.Application.Common.Realtime;
using PedagoraPilot.Infrastructure.Outbox;
using PedagoraPilot.Application.Access.Invitations;

namespace PedagoraPilot.Api.Hubs;
/// <summary>
/// Post-commit transport for Pedagora Pilot operational notifications.
/// Every outbox event with an OrganizationId is broadcast through SignalR.
/// Events carrying RecipientEmail are also queued through Itech.Emailing.
/// </summary>
public sealed class SignalROutboxTransport : IRealtimeOutboxTransport
{
    private readonly IMediator _mediator;
    private readonly IIdempotentEmailingService _emailing;
    private readonly ILogger<SignalROutboxTransport> _logger;
    private readonly IAccessInvitationProcessor _accessInvitations;
    private readonly ISignatureDeliveryProcessor _signatureDelivery;
    private readonly ISignatureArchiveProcessor _signatureArchive;
    public SignalROutboxTransport(IMediator mediator, IIdempotentEmailingService emailing, IAccessInvitationProcessor accessInvitations, ISignatureDeliveryProcessor signatureDelivery, ISignatureArchiveProcessor signatureArchive, ILogger<SignalROutboxTransport> logger)
    {
        _mediator = mediator;
        _emailing = emailing;
        _accessInvitations = accessInvitations;
        _signatureDelivery = signatureDelivery;
        _signatureArchive = signatureArchive;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxEnvelope envelope, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(envelope.PayloadJson);
        var root = document.RootElement;
        if (string.Equals(envelope.TypeKey, "pedagora.access.invitation.created.v1", StringComparison.Ordinal))
        {
            var invitationId = TryReadStrongGuid(root, "InvitationId", "invitationId");
            var token = TryReadString(root, "Token", "token");
            if (!invitationId.HasValue || string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Invalid access invitation outbox payload.");
            await _accessInvitations.ProcessCreatedAsync(invitationId.Value, token, envelope.EventId, cancellationToken);
        }
        if (string.Equals(envelope.TypeKey, "pedagora.signature.requested.v1", StringComparison.Ordinal))
        {
            var signatureId = TryReadStrongGuid(root, "EnvelopeId", "envelopeId");
            if (!signatureId.HasValue) throw new InvalidOperationException("Invalid signature-requested outbox payload.");
            await _signatureDelivery.DeliverRequestAsync(signatureId.Value, envelope.EventId, cancellationToken);
        }
        if (string.Equals(envelope.TypeKey, "pedagora.signature.reminder-requested.v1", StringComparison.Ordinal))
        {
            var signatureId = TryReadStrongGuid(root, "EnvelopeId", "envelopeId");
            if (!signatureId.HasValue) throw new InvalidOperationException("Invalid signature reminder outbox payload.");
            await _signatureDelivery.DeliverReminderAsync(signatureId.Value, envelope.EventId, cancellationToken);
        }
        if (envelope.TypeKey is "pedagora.signature.completed.v1" or "pedagora.signature.declined.v1")
        {
            var signatureId = TryReadStrongGuid(root, "EnvelopeId", "envelopeId");
            if (!signatureId.HasValue) throw new InvalidOperationException("Invalid signature-archive outbox payload.");
            await _signatureArchive.ArchiveAsync(signatureId.Value, cancellationToken);
        }
        var organizationId = TryReadGuid(root, "OrganizationId", "organizationId");
        if (organizationId.HasValue)
        {
            await _mediator.Publish(new RealtimeDomainEventNotification(organizationId.Value, envelope.EventId, envelope.TypeKey, envelope.OccurredOnUtc, envelope.PayloadJson), cancellationToken);
        }
        else
        {
            _logger.LogDebug("Outbox event {TypeKey}/{EventId} has no OrganizationId; SignalR broadcast skipped.", envelope.TypeKey, envelope.EventId);
        }

        var recipient = TryReadString(root, "RecipientEmail", "recipientEmail");
        if (string.IsNullOrWhiteSpace(recipient))
            return;
        var(subject, html, text) = BuildEmail(envelope.TypeKey, root);
        await _emailing.QueueHtmlOnceAsync(recipient, subject, html, text,
            attachments: null, tags: EmailUseCaseTags.NotificationSystem,
            idempotencyKey: $"pedagora:event:{envelope.EventId:N}",
            tenantId: organizationId, cancellationToken: cancellationToken);
    }

    private static (string Subject, string Html, string Text) BuildEmail(string typeKey, JsonElement payload)
    {
        var subject = typeKey switch
        {
            "pedagora.distance.session.created.v1" => "Pedagora Pilot — nouvelle séance distancielle",
            "pedagora.workforce.remote-work.requested.v1" => "Pedagora Pilot — demande de télétravail enregistrée",
            "pedagora.workforce.remote-work.decision-recorded.v1" => "Pedagora Pilot — décision sur votre demande de télétravail",
            _ => "Pedagora Pilot — notification"
        };
        var safeType = WebUtility.HtmlEncode(typeKey);
        var text = $"Une mise à jour Pedagora Pilot vient d'être enregistrée ({typeKey}). Connectez-vous à la plateforme pour consulter le détail.";
        var html = $"<p>Une mise à jour <strong>Pedagora Pilot</strong> vient d'être enregistrée.</p><p>Événement : <code>{safeType}</code></p><p>Connectez-vous à la plateforme pour consulter le détail.</p>";
        return (subject, html, text);
    }

    private static Guid? TryReadGuid(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id))
                return id;
        }

        return null;
    }

    private static Guid? TryReadStrongGuid(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var direct)) return direct;
            if (value.ValueKind == JsonValueKind.Object)
            {
                foreach (var childName in new[] { "Value", "value" })
                    if (value.TryGetProperty(childName, out var child) && child.ValueKind == JsonValueKind.String && Guid.TryParse(child.GetString(), out var nested)) return nested;
            }
        }
        return null;
    }

    private static string? TryReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }
}
