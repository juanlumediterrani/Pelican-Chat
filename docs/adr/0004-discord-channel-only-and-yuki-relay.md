# ADR-0004: Relevo Exclusivo del Canal Discord (`chat.message`) por **Yuki** y Contrato Real `IPelican4EverChatApi`

* **Estado:** Aceptado (actualiza el punto 1 de [ADR-0003](0003-discord-bridge-and-message-sanitization.md))
* **Fecha:** 2026-11-26
* **Ámbito:** `PelicanChat.Core/Channels/ChatRouterLogic.cs`, `PelicanChat/Modules/DiscordBridgeModule.cs`, `PelicanChat/Integration/Pelican4EverBridge.cs`, `PelicanChat/Framework/IPelican4EverChatApi.cs`

## Contexto

En el diseño inicial (`ADR-0003`, `GSS.md` `RF-24`) se contempló publicar en `HostEventLog` (`chat.message`) tanto los mensajes del canal `Global` como los del canal `Discord`, y se referenciaba al bot legado **Putre / Hermes**.

Al implementar el producto (`PelicanChat` `v0.1.0`), integrar `PelicanApi` en `Pelican4Ever` (`P4E-CHAT-01..03`) y migrar la operación Discord/AWS a **Yuki** (`DEC-0004`), tanto el dominio puro (`ChatRouterLogic.ShouldForwardToDiscord`) como el relevo externo (`Yuki` `chat_relay.py`) convergen en un contrato más estricto y limpio.

## Decisión

1. **Relevo exclusivo del canal `Discord` (`ChatChannelType.Discord`):**
   - `ChatRouterLogic.ShouldForwardToDiscord(ChatEntry)` autoriza el reenvío saliente **únicamente** cuando `entry.Origin == ExternalOrigin.None` y `entry.Channel == ChatChannelType.Discord` (es decir, mensajes enviados mediante `/d` / `/discord` o desde la pestaña `Discord`).
   - Los canales `Global`, `Local`, `Whisper` y `System` permanecen exclusivamente dentro del juego y no saturan el canal puenteado de Discord.
2. **Operador externo unificado en **Yuki** (`DEC-0004`):**
   - **Yuki** sustituye a Putre/Hermes como operador del puente Discord en AWS EC2, consumiendo `GET /api/v1/host/events?since=N` (`Type == "chat.message"` con `Data.channel == "discord"`) e inyectando mensajes entrantes mediante `POST /api/v1/host/chat`.
3. **Contrato exacto de `IPelican4EverChatApi`:**
   - La interfaz espejo consumida vía `Helper.ModRegistry.GetApi<IPelican4EverChatApi>("Pelican4Ever.SVRTMP")` fija los métodos reales expuestos por `Pelican4Ever.PelicanApi`:
     - `bool IsModEnabled()`
     - `string GetServerTimeFormatted()`
     - `void PublishChatEvent(string channel, string senderName, string message)`

## Consecuencias

* **Positivas:** Cero ruido de conversación `Global` en Discord; alineamiento exacto entre `PelicanChat.Core`, `PelicanChat.Tests`, `Pelican4Ever.PelicanApi` y el relay de **Yuki** (`DEC-0004`).
* **Negativas:** Los jugadores que deseen que un mensaje llegue a Discord deben enviarlo desde la pestaña `Discord` o con `/d` (comportamiento explícito y predecible).
