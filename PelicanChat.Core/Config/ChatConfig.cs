using System;
using System.Collections.Generic;
using PelicanChat.Core.Channels;

namespace PelicanChat.Core.Config
{
    /// <summary>
    /// Configuración serializada en <c>config.json</c> de PelicanChat.
    /// </summary>
    public sealed class ChatConfig
    {
        /// <summary>Activa o desactiva los parches visuales y de ruteo del mod.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Número máximo de mensajes retenidos en memoria por cada pestaña.</summary>
        public int MaxHistoryPerChannel { get; set; } = 200;

        /// <summary>Altura máxima en píxeles del historial cuando el ChatBox está abierto.</summary>
        public int MaxVisibleHeight { get; set; } = 280;

        /// <summary>Altura máxima en píxeles del historial cuando el ChatBox está cerrado.</summary>
        public int MaxClosedHeight { get; set; } = 140;

        /// <summary>Si se antepone la marca horaria [HH:mm] a los mensajes en la vista.</summary>
        public bool ShowTimestamps { get; set; } = true;

        /// <summary>Origen de la marca horaria: Auto, ServerClock o LocalTime.</summary>
        public string TimestampSource { get; set; } = "Auto";

        /// <summary>Incluye los mensajes de Discord dentro de la pestaña Todo.</summary>
        public bool IncludeDiscordInAllTab { get; set; } = true;

        /// <summary>Pestaña inicial al abrir el juego (All, Local, Global, Discord, Whisper, System).</summary>
        public string DefaultTab { get; set; } = nameof(ChatChannelType.All);

        /// <summary>Radio máximo en casillas (tiles) para el chat local de proximidad en modo autónomo.</summary>
        public double ProximityRadius { get; set; } = 30.0;

        /// <summary>Duración en segundos de un aviso crítico transversal (Cross-Tab Toast).</summary>
        public int CriticalToastDurationSeconds { get; set; } = 15;

        // ─────────────────────────── Puente Discord (solo Host en modo autónomo) ───────────────────────────

        /// <summary>Activa el puente Discord ligero cuando no se usa Pelican 4Ever.</summary>
        public bool DiscordBridgeEnabled { get; set; } = false;

        /// <summary>URL de Webhook de Discord (solo Host; nunca se envía a clientes).</summary>
        public string DiscordWebhookUrl { get; set; } = string.Empty;

        /// <summary>Puerto loopback local (127.0.0.1) para recibir mensajes entrantes en modo autónomo (0 = desactivado).</summary>
        public int DiscordInboundPort { get; set; } = 0;

        /// <summary>Token Bearer requerido en el listener loopback local.</summary>
        public string DiscordInboundToken { get; set; } = string.Empty;

        /// <summary>Longitud máxima permitida para mensajes entrantes desde Discord o API externa.</summary>
        public int MaxInboundMessageLength { get; set; } = 280;

        public ChatChannelType ParsedDefaultTab { get; private set; } = ChatChannelType.All;

        /// <summary>Valida la configuración y devuelve los errores encontrados.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (MaxHistoryPerChannel is < 10 or > 2000)
                errors.Add($"{nameof(MaxHistoryPerChannel)} debe estar entre 10 y 2000. Valor: {MaxHistoryPerChannel}.");

            if (MaxVisibleHeight is < 100 or > 800)
                errors.Add($"{nameof(MaxVisibleHeight)} debe estar entre 100 y 800 píxeles. Valor: {MaxVisibleHeight}.");

            if (MaxClosedHeight is < 40 or > 400)
                errors.Add($"{nameof(MaxClosedHeight)} debe estar entre 40 y 400 píxeles. Valor: {MaxClosedHeight}.");

            if (TimestampSource is not ("Auto" or "ServerClock" or "LocalTime"))
                errors.Add($"{nameof(TimestampSource)} debe ser 'Auto', 'ServerClock' o 'LocalTime'. Valor: '{TimestampSource}'.");

            if (!Enum.TryParse(DefaultTab, ignoreCase: true, out ChatChannelType parsedTab))
                errors.Add($"{nameof(DefaultTab)} no es un canal válido: '{DefaultTab}'.");
            else
                ParsedDefaultTab = parsedTab;

            if (!double.IsFinite(ProximityRadius) || ProximityRadius is < 1.0 or > 500.0)
                errors.Add($"{nameof(ProximityRadius)} debe estar entre 1 y 500 casillas. Valor: {ProximityRadius}.");

            if (CriticalToastDurationSeconds is < 0 or > 120)
                errors.Add($"{nameof(CriticalToastDurationSeconds)} debe estar entre 0 y 120 segundos. Valor: {CriticalToastDurationSeconds}.");

            if (DiscordInboundPort != 0 && DiscordInboundPort is < 1024 or > 65535)
                errors.Add($"{nameof(DiscordInboundPort)} debe ser 0 o situarse entre 1024 y 65535. Valor: {DiscordInboundPort}.");

            if (MaxInboundMessageLength is < 40 or > 1000)
                errors.Add($"{nameof(MaxInboundMessageLength)} debe estar entre 40 y 1000. Valor: {MaxInboundMessageLength}.");

            if (!string.IsNullOrWhiteSpace(DiscordWebhookUrl) &&
                (!Uri.TryCreate(DiscordWebhookUrl, UriKind.Absolute, out Uri? uri) ||
                 (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            {
                errors.Add($"{nameof(DiscordWebhookUrl)} debe ser una URL http/https válida.");
            }

            return errors;
        }
    }
}
