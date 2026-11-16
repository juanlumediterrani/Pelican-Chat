using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using PelicanChat.Core.Channels;
using PelicanChat.Core.Discord;
using PelicanChat.Framework;
using PelicanChat.Models;
using PelicanChat.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace PelicanChat.Modules
{
    /// <summary>
    /// Puente Discord del lado Host:
    /// - Si Pelican 4Ever está activo, delega los salientes a IPelican4EverChatApi.PublishChatEvent (HostEventLog).
    /// - Si opera en modo autónomo, soporta opcionalmente DiscordWebhookUrl (salida asíncrona) y loopback HTTP local.
    /// </summary>
    public sealed class DiscordBridgeModule : IModule
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

        private ModContext? context;
        private readonly ConcurrentQueue<ExternalChatOutboundEvent> outboundQueue = new();
        private readonly ConcurrentQueue<ExternalChatInboundDto> inboundQueue = new();
        private CancellationTokenSource? cts;
        private HttpListener? inboundListener;

        public string Name => "DiscordBridge";

        public ModuleSide Side => ModuleSide.Host;

        public void Register(ModContext ctx)
        {
            context = ctx;

            ChatBoxPatches.OnLocalEntryCaptured += OnEntryCaptured;
            ctx.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            ctx.Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            ctx.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnEntryCaptured(ChatEntry entry)
        {
            if (context == null || !Context.IsMainPlayer)
                return;

            if (!ChatRouterLogic.ShouldForwardToDiscord(entry))
                return;

            string channelName = entry.Channel == ChatChannelType.Discord ? "discord" : "global";

            if (context.PelicanBridge.IsPelican4EverActive)
            {
                context.PelicanBridge.PublishOutboundToHostEventLog(channelName, entry.SenderName, entry.RawText);
                return;
            }

            if (context.Config.DiscordBridgeEnabled && !string.IsNullOrWhiteSpace(context.Config.DiscordWebhookUrl))
            {
                outboundQueue.Enqueue(new ExternalChatOutboundEvent
                {
                    Channel = channelName,
                    SenderName = entry.SenderName,
                    Message = entry.RawText,
                    GameDay = Game1.Date?.TotalDays ?? 0,
                    GameTime = Game1.timeOfDay
                });
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (context == null || !Context.IsMainPlayer)
                return;

            if (context.PelicanBridge.IsPelican4EverActive || !context.Config.DiscordBridgeEnabled)
                return;

            cts = new CancellationTokenSource();
            _ = Task.Run(() => ProcessWebhookLoopAsync(cts.Token));

            if (context.Config.DiscordInboundPort > 0 && !string.IsNullOrWhiteSpace(context.Config.DiscordInboundToken))
                StartInboundListener(context.Config.DiscordInboundPort);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            StopBridge();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (context == null || !Context.IsMainPlayer || !Context.IsWorldReady)
                return;

            while (inboundQueue.TryDequeue(out ExternalChatInboundDto? dto))
            {
                string safeAuthor = ChatSanitizer.SanitizeAuthor(dto.Author);
                string safeMsg = ChatSanitizer.SanitizeForChatBox(dto.Message, context.Config.MaxInboundMessageLength);
                if (string.IsNullOrEmpty(safeMsg))
                    continue;

                string formatted = ChatRouterLogic.FormatDiscordMessage(safeAuthor, safeMsg);
                ChatBoxPatches.InjectMessage(formatted, new Color(114, 137, 218), ChatChannelType.Discord, ExternalOrigin.Discord);

                if (Game1.IsMultiplayer)
                {
                    context.Helper.Multiplayer.SendMessage(
                        new DiscordChatPayload
                        {
                            Author = safeAuthor,
                            Channel = "discord",
                            Message = safeMsg,
                            FromDiscord = true
                        },
                        ChatNetworkMessages.DiscordInboundBroadcast,
                        new[] { context.Manifest.UniqueID });
                }
            }
        }

        private async Task ProcessWebhookLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (outboundQueue.TryDequeue(out ExternalChatOutboundEvent? evt) &&
                        context != null &&
                        !string.IsNullOrWhiteSpace(context.Config.DiscordWebhookUrl))
                    {
                        string content = $"**[{evt.Channel}]** {evt.Message}";
                        string json = JsonSerializer.Serialize(new { content });
                        using var body = new StringContent(json, Encoding.UTF8, "application/json");
                        using HttpResponseMessage resp = await Http.PostAsync(context.Config.DiscordWebhookUrl, body, token);
                    }
                }
                catch
                {
                    // Nunca tumbar el juego por errores de red hacia Discord
                }

                await Task.Delay(250, token).ConfigureAwait(false);
            }
        }

        private void StartInboundListener(int port)
        {
            try
            {
                inboundListener = new HttpListener();
                inboundListener.Prefixes.Add($"http://127.0.0.1:{port}/");
                inboundListener.Start();
                _ = Task.Run(() => ListenLoopAsync(inboundListener, cts!.Token));
                context?.Monitor.Log($"[DiscordBridgeModule] Listener loopback activo en http://127.0.0.1:{port}/", LogLevel.Info);
            }
            catch (Exception ex)
            {
                context?.Monitor.Log($"[DiscordBridgeModule] No se pudo iniciar listener loopback: {ex.Message}", LogLevel.Warn);
            }
        }

        private async Task ListenLoopAsync(HttpListener listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested && listener.IsListening)
            {
                try
                {
                    HttpListenerContext httpCtx = await listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleInboundHttp(httpCtx), token);
                }
                catch
                {
                    break;
                }
            }
        }

        private void HandleInboundHttp(HttpListenerContext httpCtx)
        {
            try
            {
                string? auth = httpCtx.Request.Headers["Authorization"];
                string expected = $"Bearer {context?.Config.DiscordInboundToken}";
                if (!string.Equals(auth, expected, StringComparison.Ordinal))
                {
                    httpCtx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    httpCtx.Response.Close();
                    return;
                }

                using var reader = new StreamReader(httpCtx.Request.InputStream, Encoding.UTF8);
                string raw = reader.ReadToEnd();
                ExternalChatInboundDto? dto = JsonSerializer.Deserialize<ExternalChatInboundDto>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
                {
                    httpCtx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    httpCtx.Response.Close();
                    return;
                }

                inboundQueue.Enqueue(dto);
                httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
                httpCtx.Response.Close();
            }
            catch
            {
                try { httpCtx.Response.StatusCode = 500; httpCtx.Response.Close(); } catch { }
            }
        }

        private void StopBridge()
        {
            try { cts?.Cancel(); } catch { }
            try { inboundListener?.Stop(); inboundListener?.Close(); } catch { }
            inboundListener = null;
            cts = null;
        }
    }
}
