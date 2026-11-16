using System;
using PelicanChat.Core.Config;
using PelicanChat.Framework;
using PelicanChat.Models;
using StardewModdingAPI;
using StardewValley;

namespace PelicanChat.Integration
{
    /// <summary>
    /// Gestiona la detección y acoplamiento en tiempo de ejecución con Pelican4Ever.SVRTMP.
    /// </summary>
    public sealed class Pelican4EverBridge
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private readonly ChatConfig config;
        private IPelican4EverChatApi? api;

        public bool IsPelican4EverInstalled => api != null;

        public bool IsPelican4EverActive => api != null && SafeIsEnabled();

        public Pelican4EverBridge(IModHelper helper, IMonitor monitor, ChatConfig config)
        {
            this.helper = helper;
            this.monitor = monitor;
            this.config = config;
        }

        public void Initialize()
        {
            try
            {
                api = helper.ModRegistry.GetApi<IPelican4EverChatApi>(ChatNetworkMessages.Pelican4EverModId);
                if (api != null)
                {
                    monitor.Log(
                        $"[Pelican4EverBridge] Pelican 4Ever detectado (activo={SafeIsEnabled()}). Modo integrado habilitado.",
                        LogLevel.Info);
                }
                else
                {
                    monitor.Log(
                        "[Pelican4EverBridge] Pelican 4Ever no detectado. PelicanChat operará en modo autónomo.",
                        LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                api = null;
                monitor.Log($"[Pelican4EverBridge] Error consultando API de Pelican4Ever; modo autónomo activo: {ex.Message}", LogLevel.Warn);
            }
        }

        public string GetTimestampText()
        {
            if (!config.ShowTimestamps)
                return string.Empty;

            if (config.TimestampSource == "LocalTime")
                return $"[{DateTime.Now:HH:mm}] ";

            if ((config.TimestampSource is "Auto" or "ServerClock") && IsPelican4EverActive && api != null)
            {
                try
                {
                    string serverTime = api.GetServerTimeFormatted();
                    if (!string.IsNullOrWhiteSpace(serverTime))
                        return $"[{serverTime}] ";
                }
                catch
                {
                    // Fallback silencioso a hora del juego
                }
            }

            if (Context.IsWorldReady)
            {
                int tod = Game1.timeOfDay;
                int hours = (tod / 100) % 24;
                int mins = tod % 100;
                return $"[{hours:D2}:{mins:D2}] ";
            }

            return $"[{DateTime.Now:HH:mm}] ";
        }

        public void PublishOutboundToHostEventLog(string channel, string senderName, string message)
        {
            if (!IsPelican4EverActive || api == null)
                return;

            try
            {
                api.PublishChatEvent(channel, senderName, message);
            }
            catch (Exception ex)
            {
                monitor.Log($"[Pelican4EverBridge] No se pudo publicar chat.message en Pelican4Ever: {ex.Message}", LogLevel.Trace);
            }
        }

        private bool SafeIsEnabled()
        {
            if (api == null)
                return false;

            try
            {
                return api.IsModEnabled();
            }
            catch
            {
                return false;
            }
        }
    }
}
