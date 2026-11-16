namespace PelicanChat.Core.Discord
{
    /// <summary>
    /// Payload entrante desde un puente Discord o API local hacia el ChatBox del juego.
    /// </summary>
    public sealed class ExternalChatInboundDto
    {
        public string Source { get; set; } = "discord";
        public string Author { get; set; } = string.Empty;
        public string Channel { get; set; } = "discord";
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Evento saliente emitido desde el juego hacia Discord / HostEventLog.
    /// </summary>
    public sealed class ExternalChatOutboundEvent
    {
        public string Channel { get; set; } = "global";
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int GameDay { get; set; }
        public int GameTime { get; set; }
    }
}
