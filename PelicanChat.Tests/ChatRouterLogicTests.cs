using PelicanChat.Core.Channels;
using Xunit;

namespace PelicanChat.Tests;

public class ChatRouterLogicTests
{
    [Fact]
    public void ResolveOutgoing_OnLocalTab_RoutesPlainTextToLocal()
    {
        var res = ChatRouterLogic.ResolveOutgoing("hola vecinos", ChatChannelType.Local);
        Assert.Equal(OutgoingActionKind.SendLocal, res.Action);
        Assert.Equal("/l hola vecinos", res.CommandToExecute);
    }

    [Fact]
    public void ResolveOutgoing_OnDiscordTab_RoutesPlainTextToDiscord()
    {
        var res = ChatRouterLogic.ResolveOutgoing("hola taberna", ChatChannelType.Discord);
        Assert.Equal(OutgoingActionKind.SendDiscord, res.Action);
        Assert.Equal("/d hola taberna", res.CommandToExecute);
    }

    [Fact]
    public void ResolveOutgoing_OnSystemTab_BlocksPlainText()
    {
        var res = ChatRouterLogic.ResolveOutgoing("hola", ChatChannelType.System);
        Assert.Equal(OutgoingActionKind.BlockReadOnlySystem, res.Action);
        Assert.Null(res.CommandToExecute);
    }

    [Fact]
    public void ResolveOutgoing_ExplicitSlashCommand_OverridesActiveTab()
    {
        var res = ChatRouterLogic.ResolveOutgoing("/g hola a todos", ChatChannelType.Local);
        Assert.Equal(OutgoingActionKind.RunSlashCommand, res.Action);
        Assert.Equal("/g hola a todos", res.CommandToExecute);
    }

    [Fact]
    public void ShouldForwardToDiscord_BlocksGlobalLocalWhisperSystemAndExternalOrigins()
    {
        Assert.False(ChatRouterLogic.ShouldForwardToDiscord(new ChatEntry { Channel = ChatChannelType.Global, Origin = ExternalOrigin.None }));
        Assert.True(ChatRouterLogic.ShouldForwardToDiscord(new ChatEntry { Channel = ChatChannelType.Discord, Origin = ExternalOrigin.None }));

        Assert.False(ChatRouterLogic.ShouldForwardToDiscord(new ChatEntry { Channel = ChatChannelType.Local, Origin = ExternalOrigin.None }));
        Assert.False(ChatRouterLogic.ShouldForwardToDiscord(new ChatEntry { Channel = ChatChannelType.Whisper, Origin = ExternalOrigin.None }));
        Assert.False(ChatRouterLogic.ShouldForwardToDiscord(new ChatEntry { Channel = ChatChannelType.System, Origin = ExternalOrigin.None }));
        Assert.False(ChatRouterLogic.ShouldForwardToDiscord(new ChatEntry { Channel = ChatChannelType.Discord, Origin = ExternalOrigin.Discord }));
    }

    [Fact]
    public void IsWithinProximity_RequiresSameLocationAndDistance()
    {
        Assert.True(ChatRouterLogic.IsWithinProximity("Town", 10, 10, "Town", 20, 20, 30));
        Assert.False(ChatRouterLogic.IsWithinProximity("Town", 10, 10, "Farm", 10, 10, 30));
        Assert.False(ChatRouterLogic.IsWithinProximity("Town", 0, 0, "Town", 40, 0, 30));
    }
}
