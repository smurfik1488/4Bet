using _4Bet.Application.Realtime;
using _4BetWebApi.Hubs;

namespace _4Bet.Application.Tests;

public class RealtimeContractsTests
{
    [Fact]
    public void MatchHub_ShouldBuildExpectedEventGroupName()
    {
        var group = MatchHub.EventGroup("fixture-123");
        Assert.Equal("event:fixture-123", group);
    }

    [Fact]
    public void MatchHub_ShouldBuildExpectedUserGroupName()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var group = MatchHub.UserGroup(userId);
        Assert.Equal("user:11111111-1111-1111-1111-111111111111", group);
    }

    [Fact]
    public void SignalREventNames_ShouldStayStable()
    {
        Assert.Equal("MatchStateUpdated", SignalREventNames.MatchStateUpdated);
        Assert.Equal("OddsUpdated", SignalREventNames.OddsUpdated);
        Assert.Equal("BetAccepted", SignalREventNames.BetAccepted);
        Assert.Equal("BetSettled", SignalREventNames.BetSettled);
    }
}
