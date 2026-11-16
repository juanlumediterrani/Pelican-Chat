namespace PelicanChat.Models
{
    public static class ChatNetworkMessages
    {
        public const string LocalChat = "PelicanChat.LocalChat";
        public const string DiscordOutbound = "PelicanChat.DiscordOutbound";
        public const string DiscordInboundBroadcast = "PelicanChat.DiscordInboundBroadcast";

        // Mensajes emitidos por Pelican4Ever.SVRTMP
        public const string Pelican4EverModId = "Pelican4Ever.SVRTMP";
        public const string Pelican4EverServerBroadcast = "ServerBroadcast";
    }

    public sealed class LocalChatPayload
    {
        public long SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public float TileX { get; set; }
        public float TileY { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DiscordChatPayload
    {
        public long SenderId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Channel { get; set; } = "discord";
        public string Message { get; set; } = string.Empty;
        public bool FromDiscord { get; set; }
    }

    /// <summary>
    /// Espejo del DTO emitido por Pelican4Ever.Modules.AdminApiModule (P4E-CHAT-02/03).
    /// </summary>
    public sealed class Pelican4EverBroadcastPayload
    {
        public string Source { get; set; } = "system";
        public string Author { get; set; } = string.Empty;
        public string Channel { get; set; } = "system";
        public string Message { get; set; } = string.Empty;
        public byte ColorR { get; set; } = 255;
        public byte ColorG { get; set; } = 215;
        public byte ColorB { get; set; } = 0;
        public bool IsCritical { get; set; }
    }
}
