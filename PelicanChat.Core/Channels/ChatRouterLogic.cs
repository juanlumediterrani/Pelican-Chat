using System;

namespace PelicanChat.Core.Channels
{
    public enum OutgoingActionKind
    {
        SendGlobal,
        SendLocal,
        SendDiscord,
        ReplyWhisper,
        RunSlashCommand,
        BlockReadOnlySystem
    }

    public sealed record OutgoingResolution(
        OutgoingActionKind Action,
        string CleanText,
        string? CommandToExecute);

    public sealed record ParsedChatCommand(
        ChatChannelType? ExplicitChannel,
        string? CommandName,
        string? Target,
        string Message);

    /// <summary>
    /// Lógica pura de ruteo de mensajes salientes, comandos de canal, proximidad y prevención de eco.
    /// </summary>
    public static class ChatRouterLogic
    {
        public const double DefaultProximityRadius = 30.0;

        /// <summary>
        /// Determina si dos jugadores se encuentran en la misma ubicación y dentro del radio de proximidad.
        /// </summary>
        public static bool IsWithinProximity(
            string? senderLocation,
            double senderX,
            double senderY,
            string? receiverLocation,
            double receiverX,
            double receiverY,
            double maxDistance = DefaultProximityRadius)
        {
            if (string.IsNullOrWhiteSpace(senderLocation) || string.IsNullOrWhiteSpace(receiverLocation))
                return false;

            if (!string.Equals(senderLocation, receiverLocation, StringComparison.OrdinalIgnoreCase))
                return false;

            double dx = senderX - receiverX;
            double dy = senderY - receiverY;
            return (dx * dx) + (dy * dy) <= (maxDistance * maxDistance);
        }

        public static string FormatLocalMessage(string senderName, string message)
        {
            return $"[Local] {senderName}: {message}";
        }

        public static string FormatDiscordMessage(string author, string message)
        {
            return $"[Discord] {author}: {message}";
        }

        /// <summary>
        /// Garantiza que solo mensajes originados dentro del juego en el canal Discord salen hacia Discord,
        /// protegiendo la privacidad de Global/Local/Susurros y evitando bucles de eco.
        /// </summary>
        public static bool ShouldForwardToDiscord(ChatEntry entry)
        {
            if (entry == null)
                return false;

            if (entry.Origin != ExternalOrigin.None)
                return false;

            return entry.Channel == ChatChannelType.Discord;
        }

        /// <summary>
        /// Analiza una línea de entrada (con o sin prefijo /) para extraer el canal explícito y texto.
        /// </summary>
        public static ParsedChatCommand ParseCommand(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ParsedChatCommand(null, null, null, string.Empty);

            string trimmed = input.Trim();
            if (!trimmed.StartsWith('/'))
                return new ParsedChatCommand(null, null, null, trimmed);

            string[] parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].Substring(1).ToLowerInvariant();
            string rest = parts.Length > 1 ? trimmed.Substring(parts[0].Length).Trim() : string.Empty;

            if (command is "l" or "local")
                return new ParsedChatCommand(ChatChannelType.Local, command, null, rest);

            if (command is "g" or "global")
                return new ParsedChatCommand(ChatChannelType.Global, command, null, rest);

            if (command is "d" or "discord")
                return new ParsedChatCommand(ChatChannelType.Discord, command, null, rest);

            if (command is "r" or "reply")
                return new ParsedChatCommand(ChatChannelType.Whisper, command, null, rest);

            if (command is "w" or "whisper" or "dm" or "pm" or "message")
            {
                if (parts.Length < 2)
                    return new ParsedChatCommand(ChatChannelType.Whisper, command, null, string.Empty);

                string target = parts[1];
                string msg = parts.Length > 2 ? parts[2] : string.Empty;
                return new ParsedChatCommand(ChatChannelType.Whisper, command, target, msg);
            }

            return new ParsedChatCommand(null, command, null, trimmed);
        }

        /// <summary>
        /// Resuelve qué acción debe ejecutarse al pulsar Enter según el texto y la pestaña activa.
        /// </summary>
        public static OutgoingResolution ResolveOutgoing(string? input, ChatChannelType activeTab)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new OutgoingResolution(OutgoingActionKind.SendGlobal, string.Empty, null);

            string trimmed = input.Trim();
            if (trimmed.StartsWith('/'))
            {
                return new OutgoingResolution(OutgoingActionKind.RunSlashCommand, trimmed, trimmed);
            }

            return activeTab switch
            {
                ChatChannelType.Local => new OutgoingResolution(OutgoingActionKind.SendLocal, trimmed, $"/l {trimmed}"),
                ChatChannelType.Discord => new OutgoingResolution(OutgoingActionKind.SendDiscord, trimmed, $"/d {trimmed}"),
                ChatChannelType.Whisper => new OutgoingResolution(OutgoingActionKind.ReplyWhisper, trimmed, $"/r {trimmed}"),
                ChatChannelType.System => new OutgoingResolution(OutgoingActionKind.BlockReadOnlySystem, trimmed, null),
                _ => new OutgoingResolution(OutgoingActionKind.SendGlobal, trimmed, null)
            };
        }
    }
}
