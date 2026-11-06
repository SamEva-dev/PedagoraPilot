using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Workplace;
using Xunit;
namespace PedagoraPilot.Domain.UnitTests;
public sealed class WorkplaceTutorAssignmentTests
{
    [Fact]
    public void Scope_and_verified_identity_are_required()
    {
        var org = Guid.NewGuid(); var period = Guid.NewGuid(); var user = Guid.NewGuid();
        var x = WorkplaceTutorAssignment.Invite(org, period, Guid.NewGuid(), " TUTOR@Example.fr ");
        x.GrantsAccess(org, period, user).Should().BeFalse();
        x.Activate(user, "tutor@example.fr");
        x.GrantsAccess(org, period, user).Should().BeTrue();
        x.GrantsAccess(Guid.NewGuid(), period, user).Should().BeFalse();
        x.GrantsAccess(org, Guid.NewGuid(), user).Should().BeFalse();
        x.GrantsAccess(org, period, Guid.NewGuid()).Should().BeFalse();
        x.Revoke();
        x.GrantsAccess(org, period, user).Should().BeFalse();
    }
    [Fact]
    public void Invitation_link_is_required()
    {
        Action f = () => WorkplaceTutorAssignment.Invite(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "tutor@example.fr");
        f.Should().Throw<DomainException>().WithMessage("WORKPLACE_TUTOR_SCOPE_INVALID");
    }
    [Fact]
    public void Wrong_email_cannot_activate()
    {
        var x = WorkplaceTutorAssignment.Invite(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "tutor@example.fr");
        Action f = () => x.Activate(Guid.NewGuid(), "other@example.fr");
        f.Should().Throw<DomainException>().WithMessage("WORKPLACE_TUTOR_IDENTITY_MISMATCH");
    }
    [Fact]
    public void Pending_assignment_never_grants_access_even_to_matching_identity()
    {
        var org = Guid.NewGuid(); var period = Guid.NewGuid(); var user = Guid.NewGuid();
        var assignment = WorkplaceTutorAssignment.Invite(org, period, Guid.NewGuid(), "tutor@example.fr");
        assignment.GrantsAccess(org, period, user).Should().BeFalse();
        assignment.Status.Should().Be(WorkplaceTutorAssignmentStatus.Pending);
    }

    [Fact]
    public void Revoked_assignment_cannot_be_reactivated_with_old_invitation()
    {
        var org = Guid.NewGuid(); var period = Guid.NewGuid(); var user = Guid.NewGuid();
        var assignment = WorkplaceTutorAssignment.Invite(org, period, Guid.NewGuid(), "tutor@example.fr");
        assignment.Activate(user, "tutor@example.fr");
        assignment.Revoke();
        assignment.GrantsAccess(org, period, user).Should().BeFalse();
        Action reactivate = () => assignment.Activate(user, "tutor@example.fr");
        reactivate.Should().Throw<DomainException>().WithMessage("WORKPLACE_TUTOR_ASSIGNMENT_NOT_PENDING");
    }

    [Fact]
    public void Two_tutors_do_not_share_their_period_permissions()
    {
        var org = Guid.NewGuid(); var firstPeriod = Guid.NewGuid(); var secondPeriod = Guid.NewGuid();
        var firstUser = Guid.NewGuid(); var secondUser = Guid.NewGuid();
        var first = WorkplaceTutorAssignment.Invite(org, firstPeriod, Guid.NewGuid(), "first@example.fr");
        var second = WorkplaceTutorAssignment.Invite(org, secondPeriod, Guid.NewGuid(), "second@example.fr");
        first.Activate(firstUser, "first@example.fr");
        second.Activate(secondUser, "second@example.fr");
        first.GrantsAccess(org, firstPeriod, secondUser).Should().BeFalse();
        first.GrantsAccess(org, secondPeriod, firstUser).Should().BeFalse();
        second.GrantsAccess(org, secondPeriod, firstUser).Should().BeFalse();
        second.GrantsAccess(Guid.NewGuid(), secondPeriod, secondUser).Should().BeFalse();
    }
    [Fact]
    public void Existing_member_is_bound_to_verified_identity_without_invitation()
    {
        var organization = Guid.NewGuid(); var period = Guid.NewGuid(); var member = Guid.NewGuid();
        var assignment = WorkplaceTutorAssignment.AssignExisting(organization, period, member, "trainer@example.fr");
        assignment.InvitationId.Should().BeNull();
        assignment.Status.Should().Be(WorkplaceTutorAssignmentStatus.Active);
        assignment.GrantsAccess(organization, period, member).Should().BeTrue();
        assignment.GrantsAccess(organization, period, Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Reassignment_revoke_old_and_new_requires_authenticated_acceptance()
    {
        var org = Guid.NewGuid(); var period = Guid.NewGuid(); var oldUser = Guid.NewGuid();
        var old = WorkplaceTutorAssignment.AssignExisting(org, period, oldUser, "old@example.fr");
        old.Revoke();
        var replacement = WorkplaceTutorAssignment.Invite(org, period, Guid.NewGuid(), "new@example.fr");
        old.GrantsAccess(org, period, oldUser).Should().BeFalse();
        replacement.GrantsAccess(org, period, Guid.NewGuid()).Should().BeFalse();
        replacement.Activate(Guid.NewGuid(), "new@example.fr");
        replacement.Status.Should().Be(WorkplaceTutorAssignmentStatus.Active);
    }

    [Fact]
    public void Missing_verified_identity_is_rejected_for_direct_assignment()
    {
        Action bind = () => WorkplaceTutorAssignment.AssignExisting(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "x@example.fr");
        bind.Should().Throw<DomainException>().WithMessage("WORKPLACE_TUTOR_IDENTITY_MISMATCH");
    }
}
