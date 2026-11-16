using System;

namespace PelicanChat.Core.Channels
{
    /// <summary>
    /// Color RGBA independiente de MonoGame/XNA para mantener PelicanChat.Core 100% puro.
    /// </summary>
    public readonly record struct ChatColor(byte R, byte G, byte B, byte A = 255)
    {
        public static readonly ChatColor White = new(255, 255, 255);
        public static readonly ChatColor LocalGreen = new(130, 230, 130);
        public static readonly ChatColor GlobalCream = new(255, 255, 220);
        public static readonly ChatColor DiscordBlurple = new(114, 137, 218);
        public static readonly ChatColor WhisperCyan = new(0, 180, 180);
        public static readonly ChatColor SystemGold = new(255, 215, 0);
        public static readonly ChatColor InfoYellow = new(255, 255, 0);
        public static readonly ChatColor ErrorRed = new(220, 40, 40);
    }

    /// <summary>
    /// Entrada inmutable almacenada en el historial de PelicanChat.
    /// </summary>
    public sealed class ChatEntry
    {
        public long Id { get; init; }
        public ChatChannelType Channel { get; init; }
        public ExternalOrigin Origin { get; init; }
        public string TimestampText { get; init; } = string.Empty;
        public string SenderName { get; init; } = string.Empty;
        public string RawText { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public int ChatKind { get; init; }
        public ChatColor Color { get; init; } = ChatColor.White;
        public bool IsCritical { get; init; }
        public int MeasuredHeightPx { get; init; }
        public int TimeLeftToDisplay { get; set; } = 600;
        public float Alpha { get; set; } = 1f;
        public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    }
}
