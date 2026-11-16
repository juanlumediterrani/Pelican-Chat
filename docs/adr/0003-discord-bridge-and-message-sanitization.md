# ADR-0003: Puente Seguro con Discord y Sanitización de Mensajes Externos

* **Estado:** Aceptado
* **Fecha:** 2026-11-15

## Contexto

Conectar un mod de cliente de Stardew Valley con Discord plantea dos riesgos graves:
1. **Exposición de credenciales:** Si el mod abriera una conexión WebSocket/Gateway o llevara un `DISCORD_TOKEN` en su configuración, cualquier jugador con el modpack tendría acceso a las credenciales del bot, violando el modelo de seguridad de **Putre** (`DISCORD-BRIDGE.md`, secretos aislados en servidor).
2. **Inyección de formato en `ChatMessage.parseMessageForEmoji`:** Al inspeccionar `StardewValley.Menus.ChatMessage` (1.6.15), se ha comprobado que cualquier subcadena `[color]` (como `[red]`, `[yellow]`) altera el color del mensaje si `color == Color.White`, cualquier `[N]` con `N < EmojiMenu.totalEmojis` renderiza un sprite de emoji y corchetes mal formados alteran el troceado del texto. Además, `Fonts\SmallFont` no contiene todos los glifos Unicode ni emojis modernos de Discord.

## Decisión

1. **Prohibición de tokens de bot en clientes:** `PelicanChat` nunca incluye librerías de Discord Gateway ni almacena `DISCORD_TOKEN`. Toda comunicación con Discord pasa por el **Host**:
   - **En modo integrado con `Pelican 4Ever`:** El Host emite mensajes salientes de `Global` y `Discord` en `AdminApiModule.EventLog` (`HostEventTypes.ChatMessage = "chat.message"`), consumido por el puente externo (`hermes-putre-discord` o daemon de operaciones) vía `GET /api/v1/host/events?since=N`. Los mensajes procedentes de Discord entran exclusivamente por `POST /api/v1/host/chat` (autenticado con Bearer Token en `127.0.0.1`).
   - **En modo autónomo (sin `Pelican 4Ever`):** El Host puede configurar opcionalmente un `DiscordWebhookUrl` (solo salida en segundo plano) o un puerto loopback local protegido por token, sin que los clientes conozcan la URL ni el token.
2. **Privacidad de canales:** Los mensajes de `Local` (proximidad) y `Whisper` (susurros privados) **jamás** se publican en `HostEventLog` ni se envían a Discord.
3. **Sanitización obligatoria (`ChatSanitizer`):** Todo texto procedente de Discord o de la API HTTP pasa por `ChatSanitizer.SanitizeForChatBox` antes de entrar al motor de renderizado:
   - Neutraliza corchetes `[` y `]` para impedir la inyección de tags `[color]` o `[emoji]`.
   - Normaliza saltos de línea y recorta a `MaxInboundMessageLength = 280` caracteres.
   - Limpia caracteres fuera de rango o emojis Unicode no soportados por `SmallFont`.
4. **Prevención de bucles de eco:** Los mensajes marcados con origen `ExternalOrigin.Discord` se muestran en el `ChatBox` de todos los clientes y en la pestaña `Discord`, pero nunca se reencolan como evento saliente `chat.message`.

## Consecuencias

* **Positivas:** Cero exposición de secretos en los PCs de los jugadores; el bot de Discord (**Putre**) mantiene el control exclusivo de allowlists de servidores/canales y filtrado de privacidad; imposibilidad de romper el `SpriteFont` o el layout del `ChatBox` desde Discord.
* **Negativas:** En servidores sin `Pelican 4Ever` ni proceso puente local, el modo autónomo con Webhook solo proporciona flujo unidireccional (Juego $\rightarrow$ Discord) salvo que se levante un adaptador loopback.
