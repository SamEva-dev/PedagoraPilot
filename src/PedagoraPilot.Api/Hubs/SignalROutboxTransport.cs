using System.Net;
using System.Text.Json;
using DomainRelay.Abstractions;
using DomainRelay.EFCore.Outbox;
using Itech.Emailing.Abstractions;
using PedagoraPilot.Application.Common.Realtime;
using PedagoraPilot.Infrastructure.Outbox;

namespace PedagoraPilot.Api.Hubs;
/// <summary>
/// Post-commit transport for Pedagora Pilot operational notifications.
/// Every outbox event with an OrganizationId is broadcast through SignalR.
/// Events carrying RecipientEmail are also queued through Itech.Emailing.
/// </summary>
public sealed class SignalROutboxTransport : IRealtimeOutboxTransport
{
    private readonly IMediator _mediator;
    private readonly IEmailingService _emailing;
    private readonly ILogger<SignalROutboxTransport> _logger;
    public SignalROutboxTransport(IMediator mediator, IEmailingService emailing, ILogger<SignalROutboxTransport> logger)
    {
        _mediator = mediator;
        _emailing = emailing;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxEnvelope envelope, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(envelope.PayloadJson);
        var root = document.RootElement;
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
        await _emailing.QueueHtmlAsync(recipient, subject, html, text, attachments: null, tags: EmailUseCaseTags.NotificationSystem, cancellationToken);
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
