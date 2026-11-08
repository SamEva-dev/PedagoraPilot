using Microsoft.AspNetCore.Http;
using PedagoraPilot.Api.Middleware;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class ActiveWorkspaceAuthorizationPolicyTests
{
    [Theory]
    [InlineData(true, false, true, true, true)] // invited, active
    [InlineData(true, false, true, false, false)] // invited, revoked
    [InlineData(true, true, true, false, false)] // owner cannot bypass explicit suspension
    [InlineData(true, true, false, false, true)] // register owner prior to local projection
    [InlineData(true, false, false, false, false)] // no membership
    [InlineData(false, true, true, true, false)] // organization closed
    [InlineData(false, false, true, true, false)] // organization closed
    public void Only_eligible_active_members_or_explicit_owner_can_access(
        bool orgEnabled, bool owner, bool memberExists, bool memberActive, bool expected)
    {
        Assert.Equal(expected, WorkspaceAuthorizationPolicy.CanAccess(orgEnabled, owner, memberExists, memberActive));
    }

    [Fact]
    public void Route_id_cannot_select_another_tenant()
    {
        var context = new DefaultHttpContext();
        var selected = Guid.NewGuid();
        context.Request.RouteValues["organizationId"] = Guid.NewGuid().ToString("D");
        Assert.False(WorkspaceAuthorizationPolicy.RouteWorkspaceMatches(context, selected));
        context.Request.RouteValues["organizationId"] = selected.ToString("D");
        Assert.True(WorkspaceAuthorizationPolicy.RouteWorkspaceMatches(context, selected));
    }

    [Fact]
    public void Malformed_route_organization_is_denied()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "invalid";
        Assert.False(WorkspaceAuthorizationPolicy.RouteWorkspaceMatches(context, Guid.NewGuid()));
    }
}
