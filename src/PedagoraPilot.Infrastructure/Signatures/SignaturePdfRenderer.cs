using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Infrastructure.Signatures;

/// <summary>Snapshot-only PDF. This is a receipt, NOT a qualified electronic signature/certificate.</summary>
public sealed class SignaturePdfRenderer
{
    public SignaturePdfRenderer(IConfiguration configuration)
    {
        var type = configuration["Signatures:Pdf:QuestPdfLicense"];
        if (!Enum.TryParse<LicenseType>(type, true, out var license) || !Enum.IsDefined(license))
            throw new InvalidOperationException("Choose an approved QuestPDF license explicitly (Community/Professional/Enterprise).");
        QuestPDF.Settings.License = license;
    }

    public byte[] Render(SignatureEnvelope e)
    {
        if (e.Status is not (SignatureEnvelopeStatus.Signed or SignatureEnvelopeStatus.Declined))
            throw new InvalidOperationException("Only completed signature responses are archived.");
        using var source = JsonDocument.Parse(e.SnapshotJson);
        var lines = new List<string>();
        Flatten(source.RootElement, "", lines);
        if (lines.Count == 0) lines.Add("Contenu vide");
        var issuedAt = e.SignedAtUtc ?? e.DeclinedAtUtc ?? throw new InvalidOperationException("Response timestamp missing.");
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(38);
                page.DefaultTextStyle(t => t.FontSize(10));
                page.Header().Column(col =>
                {
                    col.Item().Text("PEDAGORAPILOT — RÉCAPITULATIF ET TRACE DE RÉPONSE").Bold().FontSize(15);
                    col.Item().PaddingTop(5).Text("Attestation de prise de connaissance / refus. Document non qualifié eIDAS.").FontSize(9);
                });
                page.Content().PaddingTop(14).Column(col =>
                {
                    void Field(string title, string value) => col.Item().PaddingVertical(2)
                        .Text($"{title} : {value}").FontSize(9);
                    Field("Enveloppe", e.Id.Value.ToString("D"));
                    Field("Organisation", e.OrganizationId.ToString("D"));
                    Field("Inscription", e.EnrollmentId.Value.ToString("D"));
                    Field("Document source", $"{e.SourceType} / {e.SourceId:D} / v{e.SourceVersion}");
                    Field("Signataire AuthGate", e.SignerAuthGateUserId.ToString("D"));
                    Field("Réponse", e.Status == SignatureEnvelopeStatus.Signed ? "Prise de connaissance confirmée" : "Refus / contestation");
                    Field("Date UTC", issuedAt.ToUniversalTime().ToString("O"));
                    Field("SHA-256 contenu exact", e.ContentSha256);
                    Field("Référence de réponse", e.EvidenceReference ?? "Sans visa : refus consigné");
                    if (!string.IsNullOrEmpty(e.ResponseComment)) Field("Commentaire du stagiaire", e.ResponseComment);
                    col.Item().PaddingTop(12).Text("CONTENU FIGÉ DU DOCUMENT").Bold();
                    foreach (var line in lines)
                        col.Item().PaddingTop(3).Text(line).FontSize(9);
                    col.Item().PaddingTop(14).Text("La prise de connaissance n'implique pas accord avec l'observation ; les horaires de présence n'ont pas été modifiés par cette réponse.")
                        .Italic().FontSize(9);
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("PedagoraPilot — preuve documentaire privée — page ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Flatten(JsonElement element, string path, List<string> output)
    {
        if (output.Count >= 12000) throw new InvalidOperationException("Source snapshot too complex to render safely.");
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    Flatten(property.Value, string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name, output);
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray()) Flatten(item, $"{path}[{index++}]", output);
                break;
            default:
                var text = element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : element.ToString();
                // Preserve the FULL content even for long comments; never truncate a signed snapshot.
                if (text.Length == 0) output.Add($"{path}: ");
                for (var offset = 0; offset < text.Length; offset += 200)
                    output.Add($"{path}{(offset > 0 ? " (suite)" : "")}: {text.Substring(offset, Math.Min(200, text.Length - offset))}");
                break;
        }
    }
}
