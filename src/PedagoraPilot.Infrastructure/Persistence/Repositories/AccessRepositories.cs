using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;

public sealed class OrganizationMemberRepository(PedagoraPilotDbContext db) : IOrganizationMemberRepository
{
    public IQueryable<OrganizationMember> Query(bool isTracking = false) => isTracking ? db.OrganizationMembers : db.OrganizationMembers.AsNoTracking();
    public Task<OrganizationMember?> GetByIdAsync(OrganizationMemberId id, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<OrganizationMember?> GetByAuthGateUserAsync(Guid organizationId, Guid authGateUserId, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.AuthGateUserId == authGateUserId, cancellationToken);
    public async Task AddAsync(OrganizationMember entity, CancellationToken cancellationToken = default) => await db.OrganizationMembers.AddAsync(entity, cancellationToken);
    public void Update(OrganizationMember entity) => db.OrganizationMembers.Update(entity);
    public void Remove(OrganizationMember entity) => db.OrganizationMembers.Remove(entity);

    public async Task SyncRoleProfilesAsync(OrganizationMember member, IReadOnlyCollection<string> roles, bool active, CancellationToken cancellationToken = default)
    {
        var normalized = roles.Select(NormalizeRole).ToHashSet(StringComparer.OrdinalIgnoreCase);
        await Sync(db.OrganizationAdministratorProfiles, member, normalized.Contains("organization_administrator"), active, () => new OrganizationAdministratorProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.OrganizationDirectionProfiles, member, normalized.Contains("organization_direction"), active, () => new OrganizationDirectionProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.SiteDirectionProfiles, member, normalized.Contains("site_direction"), active, () => new SiteDirectionProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.PedagogicalManagerProfiles, member, normalized.Contains("pedagogical_manager"), active, () => new PedagogicalManagerProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.SecretariatProfiles, member, normalized.Contains("secretariat"), active, () => new SecretariatProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.TrainerProfiles, member, normalized.Contains("trainer"), active, () => new TrainerProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.StudentProfiles, member, normalized.Contains("student"), active, () => new StudentProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.JuryProfiles, member, normalized.Contains("jury"), active, () => new JuryProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
        await Sync(db.ReadOnlyProfiles, member, normalized.Contains("read_only"), active, () => new ReadOnlyProfile(Guid.NewGuid(), member.Id, member.OrganizationId), cancellationToken);
    }

    public async Task<IReadOnlyCollection<MemberDirectoryEntry>> GetTrainerDirectoryAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var trainerIds = db.TrainerProfiles.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.IsActive).Select(x => x.MemberId);
        var managerIds = db.PedagogicalManagerProfiles.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.IsActive).Select(x => x.MemberId);
        var eligible = trainerIds.Concat(managerIds).Distinct();
        var rows = await db.OrganizationMembers.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive && eligible.Contains(x.Id))
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new MemberDirectoryEntry(x.AuthGateUserId, x.Email, x.FirstName, x.LastName, x.DisplayName,
            new[] { "trainer" })).ToArray();
    }


    public async Task<IReadOnlyCollection<string>> GetRoleCodesAsync(Guid organizationId, Guid authGateUserId, CancellationToken cancellationToken = default)
    {
        var member = await db.OrganizationMembers.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.AuthGateUserId == authGateUserId && x.IsActive)
            .Select(x => new { x.Id })
            .SingleOrDefaultAsync(cancellationToken);
        if (member is null) return Array.Empty<string>();

        var roles = new List<string>();
        if (await db.OrganizationAdministratorProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.OrganizationAdministrator");
        if (await db.OrganizationDirectionProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.OrganizationDirection");
        if (await db.SiteDirectionProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.SiteDirection");
        if (await db.PedagogicalManagerProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.PedagogicalManager");
        if (await db.SecretariatProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.Secretariat");
        if (await db.TrainerProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.Trainer");
        if (await db.StudentProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.Student");
        if (await db.JuryProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.Jury");
        if (await db.ReadOnlyProfiles.AsNoTracking().AnyAsync(x => x.MemberId == member.Id && x.IsActive, cancellationToken)) roles.Add("PedagoraPilot.ReadOnly");
        return roles;
    }

    public Task<int> CountActiveRoleAsync(Guid organizationId, string roleCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRole(roleCode);
        return normalized switch
        {
            "organization_administrator" => db.OrganizationAdministratorProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "organization_direction" => db.OrganizationDirectionProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "site_direction" => db.SiteDirectionProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "pedagogical_manager" => db.PedagogicalManagerProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "secretariat" => db.SecretariatProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "trainer" => db.TrainerProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "student" => db.StudentProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "jury" => db.JuryProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            "read_only" => db.ReadOnlyProfiles.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId && x.IsActive, cancellationToken),
            _ => Task.FromResult(0)
        };
    }

    private static string NormalizeRole(string role)
    {
        var value = (role ?? string.Empty).Trim();
        if (value.StartsWith("PedagoraPilot.", StringComparison.OrdinalIgnoreCase)) value = value["PedagoraPilot.".Length..];
        return value.Replace("OrganizationAdministrator", "organization_administrator", StringComparison.OrdinalIgnoreCase)
            .Replace("OrganizationDirection", "organization_direction", StringComparison.OrdinalIgnoreCase)
            .Replace("SiteDirection", "site_direction", StringComparison.OrdinalIgnoreCase)
            .Replace("PedagogicalManager", "pedagogical_manager", StringComparison.OrdinalIgnoreCase)
            .Replace("Secretariat", "secretariat", StringComparison.OrdinalIgnoreCase)
            .Replace("Trainer", "trainer", StringComparison.OrdinalIgnoreCase)
            .Replace("Student", "student", StringComparison.OrdinalIgnoreCase)
            .Replace("Jury", "jury", StringComparison.OrdinalIgnoreCase)
            .Replace("ReadOnly", "read_only", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task Sync<TEntity>(DbSet<TEntity> set, OrganizationMember member, bool hasRole, bool active, Func<TEntity> create, CancellationToken ct) where TEntity : class
    {
        var memberId = member.Id;
        var existing = await set.SingleOrDefaultAsync(x => EF.Property<OrganizationMemberId>(x, "MemberId") == memberId, ct);
        if (hasRole)
        {
            if (existing is null) await set.AddAsync(create(), ct);
            else typeof(TEntity).GetMethod("SetActive")!.Invoke(existing, new object[] { active });
        }
        else if (existing is not null)
        {
            typeof(TEntity).GetMethod("SetActive")!.Invoke(existing, new object[] { false });
        }
    }
}

public sealed class AccessInvitationRepository(PedagoraPilotDbContext db) : IAccessInvitationRepository
{
    public IQueryable<AccessInvitation> Query(bool isTracking = false) => isTracking ? db.AccessInvitations : db.AccessInvitations.AsNoTracking();
    public Task<AccessInvitation?> GetByIdAsync(AccessInvitationId id, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<AccessInvitation?> GetByTokenHashAsync(string tokenHash, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    public Task<bool> HasPendingAsync(Guid organizationId, string email, CancellationToken cancellationToken = default) => Query().AnyAsync(x => x.OrganizationId == organizationId && x.Email == email && x.Status == AccessInvitationStatus.Pending && x.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
    public async Task<IReadOnlyCollection<AccessInvitation>> ListForOrganizationAsync(Guid organizationId, bool isTracking = false, CancellationToken cancellationToken = default) => await Query(isTracking).Where(x => x.OrganizationId == organizationId && x.Status == AccessInvitationStatus.Pending && x.ExpiresAtUtc > DateTime.UtcNow).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
    public async Task AddAsync(AccessInvitation entity, CancellationToken cancellationToken = default) => await db.AccessInvitations.AddAsync(entity, cancellationToken);
    public void Update(AccessInvitation entity) => db.AccessInvitations.Update(entity);
    public void Remove(AccessInvitation entity) => db.AccessInvitations.Remove(entity);
}
