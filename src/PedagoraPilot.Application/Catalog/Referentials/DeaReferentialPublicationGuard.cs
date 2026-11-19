using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Catalog.Referentials;

/// <summary>
/// QA41: protection minimale contre la publication d'un référentiel sanitaire vide.
/// La présence des 11 compétences ne constitue PAS, à elle seule, une homologation
/// réglementaire. Les blocs, modules, stages et évaluations restent à contrôler.
/// </summary>
internal static class DeaReferentialPublicationGuard
{
    internal static async Task EnsureCanPublishAsync(
        TrainingProgram program,
        Guid referentialVersionId,
        IProgramFamilyRepository families,
        ICompetencyDefinitionRepository competencies,
        CancellationToken ct)
    {
        var emergencyFamily = await families.GetByCodeAsync("EMERGENCY_MEDICAL", false, ct);
        if (emergencyFamily is null || program.FamilyId != emergencyFamily.Id)
            return;

        // Ce contrôle s'applique uniquement à la famille Transport sanitaire.
        // Les familles ECSR et FIRST_AID ne changent pas de comportement.
        var professionalCount = await competencies.Query(false)
            .CountAsync(c => c.ReferentialVersionId == referentialVersionId
                && c.Active && c.Kind == CompetencyKind.Professional, ct);
        if (professionalCount < 11)
            throw new ConflictApplicationException(ErrorKeys.DeaReferentialCompetenciesIncomplete);
    }
}
