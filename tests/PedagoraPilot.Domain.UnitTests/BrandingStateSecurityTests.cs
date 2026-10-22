using PedagoraPilot.Domain.Branding;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class BrandingStateSecurityTests
{
    [Fact]
    public void Publish_Rejects_Revision_Other_Than_Draft()
    {
        var state = OrganizationBranding.Create(Guid.NewGuid());
        var draftId = Guid.NewGuid();
        state.SetDraft(draftId);
        Assert.Throws<InvalidOperationException>(() => state.Publish(Guid.NewGuid()));
        Assert.Null(state.PublishedRevisionId);
        state.Publish(draftId);
        Assert.Equal(draftId, state.PublishedRevisionId);
    }

    [Fact]
    public void Save_Draft_Does_Not_Immediately_Change_Published_Theme()
    {
        var state = OrganizationBranding.Create(Guid.NewGuid());
        var first = Guid.NewGuid();
        state.SetDraft(first);
        state.Publish(first);
        var second = Guid.NewGuid();
        state.SetDraft(second);
        Assert.Equal(first, state.PublishedRevisionId);
        Assert.Equal(second, state.DraftRevisionId);
    }
}
