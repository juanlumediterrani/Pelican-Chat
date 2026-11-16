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
}
