using PelicanChat.Core.Channels;
using PelicanChat.Core.Config;
using Xunit;

namespace PelicanChat.Tests;

public class ChatConfigTests
{
    [Fact]
    public void DefaultConfig_IsValid()
    {
        var cfg = new ChatConfig();
        var errors = cfg.Validate();
        Assert.Empty(errors);
        Assert.Equal(ChatChannelType.All, cfg.ParsedDefaultTab);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(5000)]
    public void InvalidMaxHistory_ReportsError(int value)
    {
        var cfg = new ChatConfig { MaxHistoryPerChannel = value };
        Assert.NotEmpty(cfg.Validate());
    }

    [Fact]
    public void InvalidWebhookUrl_ReportsError()
    {
        var cfg = new ChatConfig { DiscordWebhookUrl = "not-a-url" };
        Assert.NotEmpty(cfg.Validate());
    }

    [Fact]
    public void ValidHttpsWebhookUrl_IsAccepted()
    {
        var cfg = new ChatConfig { DiscordWebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        Assert.Empty(cfg.Validate());
    }

    [Theory]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(900)]
    public void InvalidMaxVisibleHeight_ReportsError(int value)
    {
        var cfg = new ChatConfig { MaxVisibleHeight = value };
        Assert.NotEmpty(cfg.Validate());
    }

    [Theory]
    [InlineData(20)]
    [InlineData(500)]
    public void InvalidMaxClosedHeight_ReportsError(int value)
    {
        var cfg = new ChatConfig { MaxClosedHeight = value };
        Assert.NotEmpty(cfg.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(600)]
    [InlineData(double.NaN)]
    public void InvalidProximityRadius_ReportsError(double value)
    {
        var cfg = new ChatConfig { ProximityRadius = value };
        Assert.NotEmpty(cfg.Validate());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(80)]
    [InlineData(70000)]
    public void InvalidDiscordInboundPort_ReportsError(int port)
    {
        var cfg = new ChatConfig { DiscordInboundPort = port };
        Assert.NotEmpty(cfg.Validate());
    }

    [Fact]
    public void ValidDiscordInboundPort_IsAccepted()
    {
        var cfg = new ChatConfig { DiscordInboundPort = 5050 };
        Assert.Empty(cfg.Validate());
    }

    [Theory]
    [InlineData("InvalidSource")]
    [InlineData("")]
    public void InvalidTimestampSource_ReportsError(string source)
    {
        var cfg = new ChatConfig { TimestampSource = source };
        Assert.NotEmpty(cfg.Validate());
    }

    [Fact]
    public void InvalidDefaultTab_ReportsError()
    {
        var cfg = new ChatConfig { DefaultTab = "NonExistent" };
        Assert.NotEmpty(cfg.Validate());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1500)]
    public void InvalidMaxInboundMessageLength_ReportsError(int length)
    {
        var cfg = new ChatConfig { MaxInboundMessageLength = length };
        Assert.NotEmpty(cfg.Validate());
    }
}
