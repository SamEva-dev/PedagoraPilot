using PedagoraPilot.Application.Signatures;
using PedagoraPilot.Domain.Signatures;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class SignatureVisibilityPolicySprintS19Tests
{
    [Theory]
    [InlineData(SignatureEnvelopeStatus.Draft, false, false)]
    [InlineData(SignatureEnvelopeStatus.Pending, false, false)]
    [InlineData(SignatureEnvelopeStatus.Viewed, false, false)]
    [InlineData(SignatureEnvelopeStatus.Signed, true, true)]
    [InlineData(SignatureEnvelopeStatus.Declined, false, true)]
    [InlineData(SignatureEnvelopeStatus.Expired, false, false)]
    [InlineData(SignatureEnvelopeStatus.Cancelled, false, false)]
    public void Colleague_and_supervisor_access_differ_for_declined_records(
        SignatureEnvelopeStatus status, bool colleagueAllowed, bool supervisorAllowed)
    {
        Assert.Equal(colleagueAllowed, SignatureVisibilityPolicy.CanReadContent(false, false, false, status));
        Assert.Equal(supervisorAllowed, SignatureVisibilityPolicy.CanReadContent(false, false, true, status));
        Assert.Equal(colleagueAllowed, SignatureVisibilityPolicy.CanListInLearnerHistory(false, false, true, false, status));
        Assert.Equal(supervisorAllowed, SignatureVisibilityPolicy.CanListInLearnerHistory(false, false, false, true, status));
    }

    [Theory]
    [InlineData(SignatureEnvelopeStatus.Draft)]
    [InlineData(SignatureEnvelopeStatus.Pending)]
    [InlineData(SignatureEnvelopeStatus.Viewed)]
    [InlineData(SignatureEnvelopeStatus.Signed)]
    [InlineData(SignatureEnvelopeStatus.Declined)]
    public void Author_may_access_their_own_observation(SignatureEnvelopeStatus status)
    {
        Assert.True(SignatureVisibilityPolicy.CanReadContent(false, true, false, status));
        Assert.True(SignatureVisibilityPolicy.CanListInLearnerHistory(false, true, false, false, status));
    }

    [Fact]
    public void Signer_does_not_see_own_unsent_draft_in_history()
    {
        Assert.False(SignatureVisibilityPolicy.CanListInLearnerHistory(true, false, false, false, SignatureEnvelopeStatus.Draft));
        Assert.True(SignatureVisibilityPolicy.CanListInLearnerHistory(true, false, false, false, SignatureEnvelopeStatus.Viewed));
    }
}
