using PelicanChat.Core.Channels;
using Xunit;

namespace PelicanChat.Tests;

public class SystemMessageClassifierTests
{
    [Fact]
    public void Classify_LocalPrefixOrGreen_ReturnsLocal()
    {
        var r1 = SystemMessageClassifier.Classify(0, "[Local] Ana: Hola", ChatColor.White);
        Assert.Equal(ChatChannelType.Local, r1.Channel);
        Assert.False(r1.IsCritical);

        var r2 = SystemMessageClassifier.Classify(0, "Hola", ChatColor.LocalGreen);
        Assert.Equal(ChatChannelType.Local, r2.Channel);
    }

    [Fact]
    public void Classify_DiscordPrefixOrBlurple_ReturnsDiscord()
    {
        var r = SystemMessageClassifier.Classify(0, "[Discord] Putre: GRRR", ChatColor.DiscordBlurple);
        Assert.Equal(ChatChannelType.Discord, r.Channel);
        Assert.False(r.IsCritical);
    }

    [Fact]
    public void Classify_WhisperKind3_ReturnsWhisper()
    {
        var r = SystemMessageClassifier.Classify(3, "Susurro secreto", ChatColor.WhisperCyan);
        Assert.Equal(ChatChannelType.Whisper, r.Channel);
    }

    [Theory]
    [InlineData("[Festival] Empieza el evento")]
    [InlineData("[Ceremonia] Comienza la ceremonia")]
    [InlineData("[Mercado] Vendido por 100g")]
    public void Classify_Pelican4EverSystemPrefixes_ReturnsSystemNonCritical(string text)
    {
        var r = SystemMessageClassifier.Classify(0, text, ChatColor.SystemGold);
        Assert.Equal(ChatChannelType.System, r.Channel);
        Assert.False(r.IsCritical);
    }

    [Fact]
    public void Classify_ControlledRestart_ReturnsSystemCritical()
    {
        var r = SystemMessageClassifier.Classify(0, "[Reinicio Controlado] En 5 minutos", ChatColor.SystemGold);
        Assert.Equal(ChatChannelType.System, r.Channel);
        Assert.True(r.IsCritical);
    }
}
