# Backlog Técnico y de Producto — *PelicanChat* (`Pelican4Ever.Chat`)

Basado en [`GSS.md`](GSS.md), [`architecture.md`](architecture.md) y los ADRs ([`docs/adr/README.md`](docs/adr/README.md)), validado sobre el decompilado de **Stardew Valley 1.6.15** y **Pelican 4Ever** (`v0.4.0`).

---

## 🛠️ 1. Toolchain y Entorno del Proyecto

* **.NET SDK 8.0 (`8.0.427` en `global.json`)**: Compila `PelicanChat.Core` y `PelicanChat` hacia `net6.0` (runtime de Stardew Valley) y ejecuta `PelicanChat.Tests` sobre `net8.0`.
* **`Pathoschild.Stardew.ModBuildConfig` (v4.3.2)**: Empaqueta y despliega automáticamente el mod en `Mods/PelicanChat` al compilar. Ruta del juego en `local.props` (copiar desde `local.props.example`).
* **ILSpy CLI (`ilspycmd`)**: Decompilador usado para verificar `StardewValley.Menus.ChatBox`, `ChatMessage` y `Game1`.
* **xUnit (`PelicanChat.Tests`)**: Suite de pruebas unitarias deterministas sobre `PelicanChat.Core` (`dotnet test`).
* **Puente de Integración**: `Pelican4Ever.SVRTMP` (`PelicanApi`, `ChatChannelModule`, `AdminApiModule`) + `Putre / Hermes` (`discord_bridge.py`).

---

## 📋 2. Backlog por Fases e Iteraciones

### Fase 0: Fundamentos, Solución Multiproyecto y Configuración
> **Objetivo:** Levantar la solución .NET multiproyecto, validar la configuración al arranque y preparar el ciclo de despliegue SMAPI y pruebas xUnit.

- [ ] **TASK-001: Solución multiproyecto (`PelicanChat.Core`, `PelicanChat`, `PelicanChat.Tests`)**
  - **Prioridad:** Crítica
  - **Descripción:** Crear `PelicanChat.sln` con `PelicanChat.Core.csproj` (`net6.0`, cero dependencias del juego), `PelicanChat.csproj` (`net6.0`, `ModBuildConfig 4.3.2`, `EnableHarmony = true`), `PelicanChat.Tests.csproj` (`net8.0`, xUnit), `manifest.json` (`UniqueID: Pelican4Ever.Chat`) y `ModEntry.cs` con registro modular aislado (`IModule`).
  - **Criterios de Aceptación:** `dotnet build PelicanChat.sln` compila con `0` advertencias (`TreatWarningsAsErrors = true`, `Nullable = enable`), `dotnet test` ejecuta la suite base y el build despliega `PelicanChat.dll` + `PelicanChat.Core.dll` en `Mods/PelicanChat`.

- [ ] **TASK-002: Configuración tipada y validador determinista (`ChatConfig.cs`)**
  - **Prioridad:** Alta
  - **Descripción:** Modelar `ChatConfig` en `PelicanChat.Core/Config/` con: `Enabled`, `MaxHistoryPerChannel` (`200`), `MaxVisibleHeight` (`280`), `MaxClosedHeight` (`140`), `ShowTimestamps` (`true`), `TimestampSource` (`Auto` / `ServerClock` / `LocalTime`), `IncludeDiscordInAllTab` (`true`), `DefaultTab` (`All`), `ProximityRadius` (`30.0`), `DiscordBridgeEnabled` (`false`), `DiscordWebhookUrl`, `DiscordInboundPort`, `DiscordInboundToken` y `MaxInboundMessageLength` (`280`). Implementar `Validate()`.
  - **Criterios de Aceptación:** `ChatConfig.Validate()` detecta valores fuera de rango (alturas < 80px, buffers < 10, puertos inválidos, radios <= 0), cubierto por tests unitarios. En `ModEntry`, una config inválida registra error en SMAPI y desactiva limpiamente el mod.

- [ ] **TASK-003: Infraestructura de parches reversibles (`HarmonyPatchToggle.cs`) e i18n**
  - **Prioridad:** Alta
  - **Descripción:** Reutilizar el patrón `HarmonyPatchToggle` para capturar todos los parches aplicados sobre `ChatBox` y poder desactivar/reactivar el mod sin reiniciar el cliente. Crear `i18n/default.json`, `i18n/es.json` y `i18n/zh-CN.json` con nombres de pestañas, ayudas de comandos y mensajes de error.
  - **Criterios de Aceptación:** Alternar `Enabled` o llamar a `Disable`/`Enable` restaura el `ChatBox` vanilla al 100% y las tres traducciones pasan test de paridad de claves.

- [ ] **TASK-004: Comando autónomo de diagnóstico y estado (`pchat_status`)**
  - **Prioridad:** Media
  - **Descripción:** Registrar comando de consola SMAPI `pchat_status` que reporte modo de ejecución (`Autónomo` vs `Integrado con Pelican 4Ever`), pestaña activa, conteo de mensajes por buffer, estado del `ScrollOffset` y estado del puente Discord.
  - **Criterios de Aceptación:** Ejecutar `pchat_status` en la consola SMAPI imprime un resumen claro sin excepciones tanto en título como en partida cargada.

---

### Fase 0.5: Spikes de Validación sobre `ChatBox` 1.6.15
> **Objetivo:** Validar en vivo dentro de Stardew Valley 1.6.15 las tres interacciones críticas con `Game1.UpdateControlInput` y `ChatMessage` antes de cerrar la UI.

- [ ] **SPIKE-01: Área clicable (`isWithinBounds`) sin disparar `clickAway()`**
  - **Pregunta:** ¿Al extender `ChatBox.isWithinBounds` hacia arriba (`-EffectiveViewportHeight - TabBarHeight`) mientras `chatBox.Selected == true`, `Game1.UpdateControlInput` entrega el clic izquierdo a `ChatBox.receiveLeftClick` sin cerrar el chat ni interactuar con el mundo detrás del panel?
  - **Criterio de Éxito:** Hacer clic sobre la zona de pestañas o de la scrollbar no dispara `clickAway()` ni usa la herramienta equipada en la mano del granjero.

- [ ] **SPIKE-02: Captura de rueda del ratón con `chatBox.Selected == true`**
  - **Pregunta:** ¿Se recibe correctamente `delta` en `ChatBox.receiveScrollWheelAction` cuando el selector de emojis está cerrado (`!choosingEmoji`), y sigue funcionando la rueda dentro de `emojiMenu` cuando `choosingEmoji == true`?
  - **Criterio de Éxito:** Mover la rueda con el chat abierto desplaza el historial cuando `!choosingEmoji`, desplaza el menú de emojis cuando `choosingEmoji` y nunca cambia el ítem seleccionado de la hotbar.

- [ ] **SPIKE-03: Comportamiento de `ChatMessage.parseMessageForEmoji` ante corchetes externos**
  - **Pregunta:** ¿Qué ocurre en `SmallFont` al inyectar `[red]`, `[999]`, `[0]`, corchetes sin cerrar `[hola` y emojis Unicode de Discord en `ChatMessage.parseMessageForEmoji`?
  - **Criterio de Éxito:** Documentar casos extremos en `PelicanChat.Tests` y confirmar que el resultado de `ChatSanitizer.SanitizeForChatBox` se renderiza sin excepciones `ArgumentException` de `SpriteFont` ni cambios de color inesperados.

---

### Iteración 1: Motor de Historial Ampliado y Scroll Suave
> **Objetivo:** Eliminar el límite de 10 mensajes, acotar la altura del cuadro y dotar al `ChatBox` de scroll fluido con rueda, teclado y barra visual nativa.

- [ ] **TASK-101: Buffer circular y estado de viewport en `PelicanChat.Core` (`ChatHistoryBuffer.cs`, `ChatViewState.cs`)**
  - **Prioridad:** Crítica
  - **Análisis Técnico:** Implementar estructura circular `O(1)` de capacidad configurable (`200` por defecto) y cálculo puro de ventana visible dada la altura de cada bloque de texto y `MaxVisibleHeight`.
  - **Criterios de Aceptación:**
    1. Insertar 250 mensajes conserva exactamente los 200 más recientes sin degradación.
    2. `ScrollByLines(delta)` y `ScrollToBottom()` acotan `ScrollOffset` entre `[0, MaxScrollOffset]`.
    3. Con `ScrollOffset > 0` (*Scroll Lock*), añadir un mensaje nuevo incrementa `ScrollOffset` en la altura/líneas correspondientes e incrementa `UnreadWhileScrolled` para que la vista no salte.

- [ ] **TASK-102: Intercepción de `receiveChatMessage` y `addMessage` en `ChatBoxPatches.cs`**
  - **Prioridad:** Crítica
  - **Análisis Técnico:** Parchear `ChatBox.receiveChatMessage` y `ChatBox.addMessage` para capturar todo mensaje (incluidos los añadidos por otros mods o `Game1.chatBox?.addInfoMessage`), construir su entrada en `ChatViewState` con sello horario opcional `[HH:mm]` y evitar pérdida tras el mensaje 10.
  - **Criterios de Aceptación:**
    1. Enviar 50 mensajes seguidos permite retroceder hasta el primero cuando el chat está abierto.
    2. Los mensajes de otros mods que llaman directamente a `Game1.chatBox?.addMessage(...)` aparecen normalmente en el historial.

- [ ] **TASK-103: Renderizador de viewport acotado, recorte vertical y barra de scroll (`ChatScrollRenderer.cs`)**
  - **Prioridad:** Crítica
  - **Análisis Técnico:** Sustituir el bucle de `ChatBox.draw` cuando `chatBox.Selected == true` para dibujar el fondo (`IClickableMenu.drawTextureBox` con `(301, 288, 15, 15)`), recortar con `GraphicsDevice.ScissorRectangle` (o renderizado exacto por línea) hasta `MaxVisibleHeight = 280px` y dibujar la scrollbar a la derecha usando sprites de `Game1.mouseCursors` (`421,459`, `421,472`, `435,463`).
  - **Criterios de Aceptación:**
    1. El cuadro de historial nunca supera `MaxVisibleHeight` ni tapa la parte superior de la pantalla.
    2. Al estar cerrado (`!chatBox.Selected`), solo se muestran mensajes recientes con `alpha > 0.01f` acotados a `MaxClosedHeight`.
    3. Arrastrar el pulgar o pulsar flechas arriba/abajo de la scrollbar desplaza el historial de forma fluida.

- [ ] **TASK-104: Atajos de teclado de scroll e indicador `"↓ N mensajes nuevos"`**
  - **Prioridad:** Alta
  - **Análisis Técnico:** Ampliar `ChatBox.receiveKeyPress` para soportar `PageUp`, `PageDown`, `Ctrl+Home`, `Ctrl+End` manteniendo `Up`/`Down` para `cheatHistory`. Dibujar una píldora inferior clicable cuando `ScrollOffset > 0 && UnreadWhileScrolled > 0`.
  - **Criterios de Aceptación:**
    1. `Up`/`Down` siguen navegando el historial de comandos `/` sin conflicto.
    2. Pulsar la píldora `"↓ N mensajes nuevos"` o `Ctrl+End` devuelve `ScrollOffset` a `0` y limpia el contador.

---

### Iteración 2: Sistema de Pestañas, Ruteo Contextual y Clasificador
> **Objetivo:** Separar la conversación en canales legibles, permitir escribir en el canal activo sin prefijos y notificar actividad en pestañas en segundo plano.

- [ ] **TASK-201: Modelo puro de pestañas y contadores (`ChatTabState.cs`, `ChatChannelType.cs`)**
  - **Prioridad:** Crítica
  - **Descripción:** Definir los canales (`All`, `Local`, `Global`, `Discord`, `Whisper`, `System`), sus colores semánticos, el filtro de pertenencia por pestaña y los contadores `UnreadCount` por pestaña no activa.
  - **Criterios de Aceptación:**
    1. Al llegar un mensaje `Local`, aumenta `UnreadCount` en `Local` si el usuario está en `Global` (y no aumenta en `All` si `All` está activa).
    2. Cambiar a una pestaña reinicia su `UnreadCount = 0`.
    3. Cubierto al 100% por pruebas unitarias en `PelicanChat.Tests`.

- [ ] **TASK-202: Renderizado e interacción de barra de pestañas (`ChatTabBarRenderer.cs`)**
  - **Prioridad:** Crítica
  - **Descripción:** Dibujar la barra de pestañas encima del borde superior del cuadro de mensajes cuando `chatBox.Selected == true`, resaltando la pestaña activa, mostrando badges `(N)` en dorado si hay mensajes sin leer y respondiendo a clic izquierdo y a `Keys.Tab` / `Shift+Tab`.
  - **Criterios de Aceptación:**
    1. Hacer clic en cada pestaña cambia el historial filtrado al instante.
    2. Pulsar `Tab` avanza a la pestaña siguiente y `Shift+Tab` retrocede a la anterior.
    3. Las pestañas respetan los textos traducidos de `i18n`.

- [ ] **TASK-203: Ruteo contextual de entrada (`ChatRouterLogic.cs` + parche en `textBoxEnter`)**
  - **Prioridad:** Alta
  - **Descripción:** Implementar lógica pura de análisis (`ResolveOutgoingCommand`) y aplicarla en `ChatBox.textBoxEnter(string)`: si no empieza por `/`, enrutar según la pestaña activa (`Local` $\rightarrow$ `/l`, `Discord` $\rightarrow$ `/d`, `Whisper` $\rightarrow$ `/r`, `All`/`Global` $\rightarrow$ global, `System` $\rightarrow$ aviso de solo lectura). Mostrar indicador del canal activo junto a la caja de texto.
  - **Criterios de Aceptación:**
    1. Escribir `"hola"` estando en la pestaña `Local` lo envía por proximidad sin requerir `/l`.
    2. Escribir `"/g hola"` estando en la pestaña `Local` lo envía igualmente al canal `Global`.
    3. Escribir en `Eventos` sin `/` muestra aviso informativo local y no filtra el mensaje a `Global`.

- [ ] **TASK-204: Clasificador de mensajes de sistema y vanilla (`SystemMessageClassifier.cs`)**
  - **Prioridad:** Alta
  - **Descripción:** Clasificar determinísticamente mensajes recibidos según `chatKind`, prefijos y color (`chatKind == 2` / amarillo, `[Local]`, `[Discord]`, `[Festival]`, `[Ceremonia]`, `[Reinicio Controlado]`, `[Mercado]`, `[Correo]`, `[Rollover]`, `[Madrugada]`, `chatKind == 3` privado, `chatKind == 1` error).
  - **Criterios de Aceptación:** Ningún mensaje de sistema, susurro o proximidad se etiqueta erróneamente como `Global` si cumple las firmas conocidas; cubierto por tests parametrizados.

- [ ] **TASK-205: Avisos críticos transversales (*Cross-Tab Pinned Toast*)**
  - **Prioridad:** Media
  - **Descripción:** Permitir que entradas marcadas como `IsCritical = true` (reinicio controlado del servidor, alerta de Rollover `02:00 AM`) se muestren durante `15s` en una banda superior sutil aunque el jugador tenga seleccionada la pestaña `Local` o `Susurros`.
  - **Criterios de Aceptación:** Un aviso `[Reinicio Controlado]` es visible para el jugador en cualquier pestaña activa.

---

### Iteración 3: Puente con Canal de Discord y Sanitización
> **Objetivo:** Preparar la comunicación bidireccional segura con Discord sin exponer tokens en los clientes ni permitir inyecciones sobre `ChatMessage`.

- [ ] **TASK-301: Sanitizador de mensajes externos (`ChatSanitizer.cs`)**
  - **Prioridad:** Crítica
  - **Descripción:** Implementar en `PelicanChat.Core` el saneamiento de autor y cuerpo: reemplazo de corchetes `[` / `]` por `(` / `)`, conversión de menciones Discord (`<@id>`, `<#id>`), recorte a `MaxInboundMessageLength`, normalización de saltos de línea y filtrado de caracteres no imprimibles / emojis Unicode fuera de `SmallFont`.
  - **Criterios de Aceptación:** Tests unitarios verifican neutralización de `[red]`, `[0]`, `[999]`, cadenas > 500 chars, saltos `\r\n` múltiples y caracteres surrogate pairs (emojis Unicode).

- [ ] **TASK-302: Canal y comando `/d` (`/discord`) y transporte SMAPI de canal Discord**
  - **Prioridad:** Alta
  - **Descripción:** Registrar comando `/d` y `/discord` en `ChatCommands` (si no existe). Cuando un cliente escribe en `Discord` o usa `/d <texto>`, enviarlo al Host vía SMAPI (`PelicanChat.DiscordOutbound`), quien lo difunde a los demás jugadores conectados con color `#7289DA` y lo expone al puente Discord del Host.
  - **Criterios de Aceptación:** Dos clientes en co-op ven mutuamente sus mensajes de `/d` en la pestaña `Discord` (y `Todo` si `IncludeDiscordInAllTab = true`).

- [ ] **TASK-303: Puente autónomo de Discord en Host (`DiscordBridgeModule.cs`)**
  - **Prioridad:** Media
  - **Descripción:** Cuando **Pelican 4Ever** no está activo y `DiscordBridgeEnabled == true` en el Host:
    - Cola asíncrona en segundo plano hacia `DiscordWebhookUrl` para mensajes de `Global`/`Discord` (con rate limiting y prevención de eco).
    - Listener HTTP loopback opcional (`127.0.0.1:DiscordInboundPort`) autenticado por Bearer Token (`DiscordInboundToken`) para recibir mensajes entrantes desde un bot externo.
  - **Criterios de Aceptación:** Un fallo de red o timeout del webhook de Discord **nunca** congela el hilo principal del juego (`UpdateTicked`) ni expone URLs/tokens en el log o a los clientes.

- [ ] **TASK-304: Prevención de bucles de eco y filtrado de privacidad**
  - **Prioridad:** Crítica
  - **Descripción:** Garantizar en `PelicanChat.Core` (`ExternalChatContract` / `ChatRouterLogic`) que mensajes con `Origin == Discord` no disparen reenvío saliente y que `Local` / `Whisper` nunca pasen al puente de Discord.
  - **Criterios de Aceptación:** Tests unitarios verifican `ShouldForwardToDiscord(entry) == false` para `Local`, `Whisper`, `System` y cualquier entrada con `Origin == ExternalOrigin.Discord`.

---

### Iteración 4: Integración Plena con `Pelican 4Ever`
> **Objetivo:** Lograr acoplamiento transparente, sin colisiones y enriquecido entre `PelicanChat` y `Pelican4Ever.SVRTMP`.

- [ ] **TASK-401: Puente de integración y detección en vivo (`Pelican4EverBridge.cs`, `IPelican4EverChatApi.cs`)**
  - **Prioridad:** Crítica
  - **Descripción:** En `GameLaunched`, detectar `Helper.ModRegistry.GetApi<IPelican4EverChatApi>("Pelican4Ever.SVRTMP")`. Si está presente y activo, activar el modo integrado; en caso contrario, activar `ChatTransportModule` autónomo (`/l`, `/g`, `/w`).
  - **Criterios de Aceptación:** Con `Pelican4Ever` instalado, no se produce duplicidad de comandos en `ChatCommands` ni doble envío de mensajes `/l`; sin `Pelican4Ever`, `PelicanChat` opera autónomamente con `/l`, `/g`, `/w` y `/d`.

- [ ] **TASK-402: Sincronización de marcas horarias `[HH:mm]` del reloj 1:1**
  - **Prioridad:** Media
  - **Descripción:** Consultar la lectura del reloj 1:1 a través de `IPelican4EverChatApi` (o derivar de `Game1.timeOfDay` / hora local en modo autónomo) para prefijar las entradas del historial con `[HH:mm]` y distinguir madrugada (`2550` / `NightHold`).
  - **Criterios de Aceptación:** Cada entrada muestra la hora exacta coherente con el servidor de `Pelican 4Ever` (`06:00`–`01:50` y madrugada).

- [ ] **TASK-403: Clasificación de eventos de módulos de `Pelican 4Ever`**
  - **Prioridad:** Alta
  - **Descripción:** Configurar `SystemMessageClassifier` para reconocer todos los prefijos y mensajes emitidos por `Pelican 4Ever` (`FestivalParticipationModule`, `FestivalMinigameModule`, `DailyRolloverModule`, `CommunityMarketModule`, `MailOrderModule`, `AdminApiModule`) y enrutarlos a `Eventos` (con `IsCritical = true` en avisos de reinicio y rollover).
  - **Criterios de Aceptación:** Anuncios de festivales, mercado, rollover y reinicios aparecen en la pestaña `Eventos` y no contaminan `Local` ni `Susurros`.

- [ ] **TASK-404: Compatibilidad con `Pelican4Ever.Enabled = false` (`ModToggleModule`)**
  - **Prioridad:** Media
  - **Descripción:** Comprobar dinámicamente `api.IsModEnabled()` en `SaveLoaded` / cambios de estado. Si `Pelican 4Ever` está deshabilitado en el cliente (por ejemplo para jugar otra partida), `PelicanChat` conmuta a modo autónomo sin errores.
  - **Criterios de Aceptación:** Desactivar `Pelican 4Ever` con `p4e_toggle off` o en `config.json` deja a `PelicanChat` funcionando en modo autónomo sin excepciones.

- [ ] **TASK-405: Documentación de despliegue con `RequiredClientMods` y pruebas de integración**
  - **Prioridad:** Media
  - **Descripción:** Documentar en `README.md` y validar en pruebas la incorporación de `"Pelican4Ever.Chat"` al modpack y a `RequiredClientMods` (opcional u obligatorio) en el servidor dedicado.
  - **Criterios de Aceptación:** Verificado el arranque conjunto de `Pelican4Ever.SVRTMP` + `Pelican4Ever.Chat` en host y clientes sin conflictos de `UniqueID` ni advertencias en log de SMAPI.

---

### Tareas de Puente en el Repositorio `Pelican 4Ever` (`Stardew Valley MORPG`)
> **Nota:** Estas 3 tareas se aplican en el repositorio hermano `Stardew Valley MORPG` (`Pelican4Ever`) para habilitar la comunicación oficial de API y la retransmisión hacia los clientes.

- [ ] **P4E-CHAT-01: Ampliar `PelicanApi.cs` con contrato de chat y reloj**
  - **Archivo destino (`Pelican4Ever`):** `Pelican4Ever/PelicanApi.cs`
  - **Descripción:** Exponer `IsModEnabled()`, `GetClockTimeFormatted()` / lectura horaria, método `PublishChatEvent(string channel, string senderName, string message)` (para alimentar `HostEventLog` desde `PelicanChat` en el Host) y soporte para suscriptores de anuncios de servidor.
  - **Criterios de Aceptación:** `PelicanChat` puede consumir la API mediante su interfaz espejo `IPelican4EverChatApi` usando `Helper.ModRegistry.GetApi` sin referenciar `Pelican4Ever.dll`.

- [ ] **P4E-CHAT-02: Corregir retransmisión Host $\rightarrow$ Clientes en `AdminApiModule.BroadcastToAllPlayers`**
  - **Archivo destino (`Pelican4Ever`):** `Pelican4Ever/Modules/AdminApiModule.cs` y `Pelican4Ever/Models/ChatModels.cs`
  - **Descripción:** Hacer que `BroadcastToAllPlayers(message, channel)` envíe un mensaje SMAPI (`ChatMessages.ServerBroadcast`) a todos los clientes conectados además de invocar `Game1.chatBox?.addMessage(...)` en local (actualmente invisible en servidores headless).
  - **Criterios de Aceptación:** Al llamar a `POST /api/v1/host/announce` o disparar un reinicio controlado en un host headless, todos los *farmhands* reciben el mensaje en su `ChatBox` (y en la pestaña `Eventos` si usan `PelicanChat`).

- [ ] **P4E-CHAT-03: Añadir `POST /api/v1/host/chat` y `HostEventTypes.ChatMessage`**
  - **Archivos destino (`Pelican4Ever`):** `Pelican4Ever.Core/Admin/HostEventLog.cs`, `Pelican4Ever.Core/Admin/HostApiContract.cs`, `Pelican4Ever/Modules/AdminApiModule.cs`, `docs/host-api.md`
  - **Descripción:** Añadir `HostEventTypes.ChatMessage = "chat.message"`, registrar el suceso cuando entren mensajes en `Global` o `Discord` (nunca `Local` ni `Whisper`), y añadir la ruta `POST /api/v1/host/chat` (`HostChatRequestDto`) autenticada por Bearer Token para inyectar mensajes desde Discord/Putre en el hilo principal del juego y difundirlos a todos los clientes.
  - **Criterios de Aceptación:** `HostApiCatalog.ExtractDocumentedRoutes` sigue pasando los tests de paridad en `Pelican4Ever.Tests`, `GET /api/v1/host/events?since=N` entrega eventos `chat.message` en orden estricto y `POST /api/v1/host/chat` difunde el mensaje saneado a todos los clientes conectados.
