using PelicanChat.Core.Discord;
using Xunit;

namespace PelicanChat.Tests;

public class ChatSanitizerTests
{
    [Fact]
    public void SanitizeForChatBox_NeutralizesColorAndEmojiBrackets()
    {
        string result = ChatSanitizer.SanitizeForChatBox("Hola [red]mundo [12] [sinCerrar");
        Assert.Equal("Hola (red)mundo (12) (sinCerrar", result);
        Assert.DoesNotContain("[", result);
        Assert.DoesNotContain("]", result);
    }

    [Fact]
    public void SanitizeForChatBox_ReplacesDiscordMentionsAndSurrogateEmojis()
    {
        string result = ChatSanitizer.SanitizeForChatBox("Hey <@!12345> mira <#999> <:stardrop:42> 🍓🔥");
        Assert.Equal("Hey @usuario mira #canal :stardrop:", result);
    }

    [Fact]
    public void SanitizeForChatBox_TruncatesLongMessagesAndNormalizesNewlines()
    {
        string longText = "Linea1\r\nLinea2\t" + new string('a', 300);
        string result = ChatSanitizer.SanitizeForChatBox(longText, maxLength: 50);
        Assert.True(result.Length <= 50);
        Assert.EndsWith("...", result);
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\r", result);
    }
}
