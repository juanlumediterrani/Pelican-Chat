using System;
using System.Linq;
using Microsoft.Xna.Framework;
using PelicanChat.Core.Channels;
using PelicanChat.Core.Discord;
using PelicanChat.Framework;
using PelicanChat.Models;
using PelicanChat.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace PelicanChat.Modules
{
    /// <summary>
    /// Gestiona comandos de canal (/d siempre; /l, /g, /w en modo autónomo) y recepción de mensajes SMAPI
    /// incluyendo los ServerBroadcast emitidos por Pelican4Ever.SVRTMP.
    /// </summary>
    public sealed class ChatTransportModule : IModule
    {
        private ModContext? context;
        private bool commandsChecked;

        public string Name => "ChatTransport";

        public ModuleSide Side => ModuleSide.Both;

        public void Register(ModContext ctx)
        {
            context = ctx;

            ctx.Helper.Events.GameLoop.GameLaunched += (_, _) => EnsureCommandsRegistered();
            ctx.Helper.Events.GameLoop.SaveLoaded += (_, _) => EnsureCommandsRegistered();
            ctx.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private void EnsureCommandsRegistered()
        {
            if (context == null || commandsChecked)
                return;

            try
            {
                // /d y /discord siempre se registran si nadie los ha tomado
                if (!ChatCommands.Exists("discord"))
                {
                    ChatCommands.Register(
                        "discord",
                        HandleDiscordCommand,
                        _ => I18n.Get("chat.discord_help"),
                        new[] { "d" },
                        mainOnly: false,
                        multiplayerOnly: false,
                        cheatsOnly: false);
                }

                // Si Pelican 4Ever NO está activo, PelicanChat provee /l, /g y /w autónomamente
                if (!context.PelicanBridge.IsPelican4EverActive)
                {
                    if (!ChatCommands.Exists("w"))
                        ChatCommands.RegisterAlias("w", "message");

                    if (!ChatCommands.Exists("local"))
                    {
                        ChatCommands.Register(
                            "local",
                            HandleLocalCommand,
                            _ => I18n.Get("chat.local_help"),
                            new[] { "l" },
                            mainOnly: false,
                            multiplayerOnly: true,
                            cheatsOnly: false);
                    }

                    if (!ChatCommands.Exists("global"))
                    {
                        ChatCommands.Register(
                            "global",
                            HandleGlobalCommand,
                            _ => I18n.Get("chat.global_help"),
                            new[] { "g" },
                            mainOnly: false,
                            multiplayerOnly: true,
                            cheatsOnly: false);
                    }
                }

                commandsChecked = true;
            }
            catch (Exception ex)
            {
                context.Monitor.Log($"[ChatTransportModule] Error registrando comandos: {ex.Message}", LogLevel.Warn);
            }
        }

        private void HandleLocalCommand(string[] command, ChatBox chat)
        {
            if (context == null)
                return;

            if (command.Length <= 1)
            {
                chat.addInfoMessage(I18n.Get("chat.local_usage"));
                return;
            }

            string text = string.Join(" ", command.Skip(1));
            if (string.IsNullOrWhiteSpace(text))
                return;

            var payload = new LocalChatPayload
            {
                SenderId = Game1.player.UniqueMultiplayerID,
                SenderName = Game1.player.Name,
                LocationName = Game1.currentLocation?.NameOrUniqueName ?? string.Empty,
                TileX = Game1.player.TilePoint.X,
                TileY = Game1.player.TilePoint.Y,
                Message = text
            };

            context.Helper.Multiplayer.SendMessage(
                payload,
                ChatNetworkMessages.LocalChat,
                new[] { context.Manifest.UniqueID });

            string formatted = ChatRouterLogic.FormatLocalMessage(payload.SenderName, payload.Message);
            ChatBoxPatches.InjectMessage(formatted, new Color(130, 230, 130), ChatChannelType.Local, ExternalOrigin.None);
        }

        private static void HandleGlobalCommand(string[] command, ChatBox chat)
        {
            if (command.Length <= 1)
            {
                chat.addInfoMessage(I18n.Get("chat.global_usage"));
                return;
            }

            string text = string.Join(" ", command.Skip(1));
            if (!string.IsNullOrWhiteSpace(text))
                chat.textBoxEnter(text);
        }

        private void HandleDiscordCommand(string[] command, ChatBox chat)
        {
            if (context == null)
                return;

            if (command.Length <= 1)
            {
                chat.addInfoMessage(I18n.Get("chat.discord_usage"));
                return;
            }

            string text = string.Join(" ", command.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            string author = Game1.player?.Name ?? "Farmer";
            var payload = new DiscordChatPayload
            {
                SenderId = Game1.player?.UniqueMultiplayerID ?? 0,
                Author = author,
                Channel = "discord",
                Message = text,
                FromDiscord = false
            };

            if (Game1.IsMultiplayer)
            {
                context.Helper.Multiplayer.SendMessage(
                    payload,
                    ChatNetworkMessages.DiscordOutbound,
                    new[] { context.Manifest.UniqueID });
            }

            string formatted = ChatRouterLogic.FormatDiscordMessage(author, text);
            ChatBoxPatches.InjectMessage(
                formatted,
                new Color(114, 137, 218),
                ChatChannelType.Discord,
                ExternalOrigin.None,
                senderName: author,
                cleanBody: text);
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (context == null)
                return;

            // 1) Mensajes emitidos por Pelican4Ever.SVRTMP (anuncios de servidor / Discord / Host)
            if (string.Equals(e.FromModID, ChatNetworkMessages.Pelican4EverModId, StringComparison.OrdinalIgnoreCase) &&
                e.Type == ChatNetworkMessages.Pelican4EverServerBroadcast)
            {
                var p4e = e.ReadAs<Pelican4EverBroadcastPayload>();
                if (p4e == null || string.IsNullOrWhiteSpace(p4e.Message))
                    return;

                bool fromDiscord = string.Equals(p4e.Source, "discord", StringComparison.OrdinalIgnoreCase);
                ChatChannelType channel = fromDiscord ? ChatChannelType.Discord : ChatChannelType.System;
                ExternalOrigin origin = fromDiscord ? ExternalOrigin.Discord : ExternalOrigin.HostAgent;

                string text = fromDiscord
                    ? ChatRouterLogic.FormatDiscordMessage(
                        ChatSanitizer.SanitizeAuthor(p4e.Author),
                        ChatSanitizer.SanitizeForChatBox(p4e.Message, context.Config.MaxInboundMessageLength))
                    : p4e.Message;

                var color = new Color(p4e.ColorR, p4e.ColorG, p4e.ColorB);
                ChatBoxPatches.InjectMessage(text, color, channel, origin, p4e.IsCritical);
                return;
            }

            if (e.FromModID != context.Manifest.UniqueID)
                return;

            // 2) Chat local autónomo
            if (e.Type == ChatNetworkMessages.LocalChat)
            {
                var payload = e.ReadAs<LocalChatPayload>();
                if (payload == null || payload.SenderId == Game1.player.UniqueMultiplayerID)
                    return;

                string myLoc = Game1.currentLocation?.NameOrUniqueName ?? string.Empty;
                bool inRange = ChatRouterLogic.IsWithinProximity(
                    payload.LocationName,
                    payload.TileX,
                    payload.TileY,
                    myLoc,
                    Game1.player.TilePoint.X,
                    Game1.player.TilePoint.Y,
                    context.Config.ProximityRadius);

                if (inRange)
                {
                    string formatted = ChatRouterLogic.FormatLocalMessage(payload.SenderName, payload.Message);
                    ChatBoxPatches.InjectMessage(formatted, new Color(130, 230, 130), ChatChannelType.Local, ExternalOrigin.None);
                }
            }
            // 3) Mensajes /d entre jugadores
            else if (e.Type == ChatNetworkMessages.DiscordOutbound)
            {
                var payload = e.ReadAs<DiscordChatPayload>();
                if (payload == null || payload.SenderId == Game1.player.UniqueMultiplayerID)
                    return;

                string formatted = ChatRouterLogic.FormatDiscordMessage(payload.Author, payload.Message);
                ChatBoxPatches.InjectMessage(
                    formatted,
                    new Color(114, 137, 218),
                    ChatChannelType.Discord,
                    ExternalOrigin.None,
                    senderName: payload.Author,
                    cleanBody: payload.Message);
            }
            // 4) Mensajes entrantes desde puente Discord autónomo
            else if (e.Type == ChatNetworkMessages.DiscordInboundBroadcast)
            {
                var payload = e.ReadAs<DiscordChatPayload>();
                if (payload == null)
                    return;

                string safeAuthor = ChatSanitizer.SanitizeAuthor(payload.Author);
                string safeMsg = ChatSanitizer.SanitizeForChatBox(payload.Message, context.Config.MaxInboundMessageLength);
                if (string.IsNullOrEmpty(safeMsg))
                    return;

                string formatted = ChatRouterLogic.FormatDiscordMessage(safeAuthor, safeMsg);
                ChatBoxPatches.InjectMessage(formatted, new Color(114, 137, 218), ChatChannelType.Discord, ExternalOrigin.Discord);
            }
        }
    }
}
