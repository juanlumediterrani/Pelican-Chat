# Backlog Técnico y de Producto — *PelicanChat* (`Pelican4Ever.Chat`)

Basado en [`GSS.md`](GSS.md), [`architecture.md`](architecture.md) y los ADRs ([`docs/adr/README.md`](docs/adr/README.md)), validado sobre el decompilado de **Stardew Valley 1.6.15** y **Pelican 4Ever** (`v0.4.0+`).

> **Gobernanza (`EST-001` v1.2 / `DEC-0001`):** Este archivo recoge la traza técnica local de implementación y aceptación de `PelicanChat`; la fuente corporativa viva de tareas de empresa es **ERPNext** (`PROJ-0005`).

---

## 🛠️ 1. Toolchain y Entorno del Proyecto

* **.NET SDK 8.0 (`8.0.427` en `global.json`)**: Compila `PelicanChat.Core` y `PelicanChat` hacia `net6.0` (runtime de Stardew Valley) y ejecuta `PelicanChat.Tests` sobre `net8.0`.
* **`Pathoschild.Stardew.ModBuildConfig` (v4.3.2)**: Empaqueta y despliega el mod en `Mods/PelicanChat` (en CI y validaciones locales usar `-p:EnableModDeploy=false -p:EnableModZip=false`). Ruta del juego en `local.props` (copiar desde `local.props.example`).
* **ILSpy CLI (`ilspycmd`)**: Decompilador usado para verificar `StardewValley.Menus.ChatBox`, `ChatMessage` y `Game1`.
* **xUnit (`PelicanChat.Tests`)**: Suite de pruebas unitarias deterministas sobre `PelicanChat.Core`, paridad `i18n` y contratos del Project Brain (`dotnet test`).
* **Guardas del Brain y Harmony**: `scripts/verify_brain.py --check` (`EST-001` v1.2) y `../MCP Pelican/skills/smapi-harmony-pelican-bridge/scripts/check_harmony_patches.py . --strict`.
* **Puente de Integración**: `Pelican4Ever.SVRTMP` (`PelicanApi`, `ChatChannelModule`, `AdminApiModule`) + **Yuki** (`DEC-0004`, Discord + ops AWS).

---

## 📋 2. Backlog por Fases e Iteraciones

### Fase 0: Fundamentos, Solución Multiproyecto y Configuración
> **Objetivo:** Levantar la solución .NET multiproyecto, validar la configuración al arranque y preparar el ciclo de despliegue SMAPI y pruebas xUnit.

- [x] **TASK-001: Solución multiproyecto (`PelicanChat.Core`, `PelicanChat`, `PelicanChat.Tests`)**
  - **Prioridad:** Crítica
  - **Descripción:** Crear `PelicanChat.sln` con `PelicanChat.Core.csproj` (`net6.0`, cero dependencias del juego), `PelicanChat.csproj` (`net6.0`, `ModBuildConfig 4.3.2`, `EnableHarmony = true`), `PelicanChat.Tests.csproj` (`net8.0`, xUnit), `manifest.json` (`UniqueID: Pelican4Ever.Chat`) y `ModEntry.cs` con registro modular aislado (`IModule`).
  - **Criterios de Aceptación:** `dotnet build PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false` compila con `0` advertencias (`TreatWarningsAsErrors = true`, `Nullable = enable`) y `dotnet test` ejecuta la suite completa.

- [x] **TASK-002: Configuración tipada y validador determinista (`ChatConfig.cs`)**
  - **Prioridad:** Alta
  - **Descripción:** Modelar `ChatConfig` en `PelicanChat.Core/Config/` con: `Enabled`, `MaxHistoryPerChannel` (`200`), `MaxVisibleHeight` (`280`), `MaxClosedHeight` (`140`), `ShowTimestamps` (`true`), `TimestampSource` (`Auto` / `ServerClock` / `LocalTime`), `IncludeDiscordInAllTab` (`true`), `DefaultTab` (`All`), `ProximityRadius` (`30.0`), `CriticalToastDurationSeconds` (`15`), `DiscordBridgeEnabled` (`false`), `DiscordWebhookUrl`, `DiscordInboundPort`, `DiscordInboundToken` y `MaxInboundMessageLength` (`280`). Implementar `Validate()`.
  - **Criterios de Aceptación:** `ChatConfig.Validate()` detecta valores fuera de rango (`MaxVisibleHeight` fuera de `100..800`, `MaxClosedHeight` fuera de `40..400`, buffers `< 10`, puertos inválidos, radios `<= 0` o no finitos), cubierto por `ChatConfigTests.cs`. En `ModEntry`, una config inválida registra error en SMAPI y desactiva limpiamente el mod.

- [x] **TASK-003: Infraestructura de parches reversibles (`HarmonyPatchToggle.cs`) e i18n**
  - **Prioridad:** Alta
  - **Descripción:** Reutilizar el patrón `HarmonyPatchToggle` para capturar todos los parches aplicados sobre `ChatBox` y poder desactivar/reactivar el mod sin reiniciar el cliente. Crear `i18n/default.json`, `i18n/es.json` y `i18n/zh-CN.json` con nombres de pestañas, ayudas de comandos y mensajes de error.
  - **Criterios de Aceptación:** `HarmonyPatchToggle` captura y restaura los parches propios sobre `ChatBox`, y las tres traducciones pasan `I18nParityTests` en `PelicanChat.Tests`.

- [x] **TASK-004: Comando autónomo de diagnóstico y estado (`pchat_status`)**
  - **Prioridad:** Media
  - **Descripción:** Registrar comando de consola SMAPI `pchat_status` que reporte modo de ejecución (`Autónomo` vs `Integrado con Pelican 4Ever`), pestaña activa, conteo de mensajes por buffer, estado del `ScrollOffset` y estado del puente Discord.
  - **Criterios de Aceptación:** Implementado en `ChatViewModule.OnStatusCommand` tanto en título como en partida cargada.

---

### Fase 0.5: Spikes de Validación sobre `ChatBox` 1.6.15
> **Objetivo:** Validar en vivo dentro de Stardew Valley 1.6.15 las tres interacciones críticas con `Game1.UpdateControlInput` y `ChatMessage` antes de cerrar la UI.

- [ ] **SPIKE-01: Área clicable (`isWithinBounds`) sin disparar `clickAway()`**
  - **Pregunta:** ¿Al extender `ChatBox.isWithinBounds` hacia arriba (`-EffectiveViewportHeight - TabBarHeight`) mientras `chatBox.Selected == true`, `Game1.UpdateControlInput` entrega el clic izquierdo a `ChatBox.receiveLeftClick` sin cerrar el chat ni interactuar con el mundo detrás del panel?
  - **Estado:** *Parcial* — Verificado estáticamente en el decompilado (`architecture.md` §2.2) e implementado en `ChatBoxPatches.Postfix_IsWithinBounds`; pendiente informe de prueba en vivo en `docs/verifications/`.

- [ ] **SPIKE-02: Captura de rueda del ratón con `chatBox.Selected == true`**
  - **Pregunta:** ¿Se recibe correctamente `delta` en `ChatBox.receiveScrollWheelAction` cuando el selector de emojis está cerrado (`!choosingEmoji`), y sigue funcionando la rueda dentro de `emojiMenu` cuando `choosingEmoji == true`?
  - **Estado:** *Parcial* — Verificado estáticamente en el decompilado (`architecture.md` §2.1) e implementado en `ChatBoxPatches.Postfix_ReceiveScrollWheelAction`; pendiente informe de prueba en vivo en `docs/verifications/`.

- [ ] **SPIKE-03: Comportamiento de `ChatMessage.parseMessageForEmoji` ante corchetes externos**
  - **Pregunta:** ¿Qué ocurre en `SmallFont` al inyectar `[red]`, `[999]`, `[0]`, corchetes sin cerrar `[hola` y emojis Unicode de Discord en `ChatMessage.parseMessageForEmoji`?
  - **Estado:** *Parcial* — Casos extremos cubiertos en `ChatSanitizerTests.cs` (`[red]`, `[0]`, `[999]`, `[sinCerrar`, pares subrogados); pendiente verificación visual en vivo en `docs/verifications/`.

---

### Iteración 1: Motor de Historial Ampliado y Scroll Suave
> **Objetivo:** Eliminar el límite de 10 mensajes, acotar la altura del cuadro y dotar al `ChatBox` de scroll fluido con rueda, teclado y barra visual nativa.

- [x] **TASK-101: Buffer circular y estado de viewport en `PelicanChat.Core` (`ChatHistoryBuffer.cs`, `ChatViewState.cs`)**
  - **Prioridad:** Crítica
  - **Análisis Técnico:** Estructura acotada de capacidad configurable (`200` por defecto) y cálculo puro de ventana visible dada la altura de cada bloque de texto y `MaxVisibleHeight`.
  - **Criterios de Aceptación:** Cubierto por `ChatViewStateTests.cs` (tope de buffer, acotación de `ScrollOffsetPx` y *Scroll Lock* con `UnreadWhileScrolled`).

- [x] **TASK-102: Intercepción de `receiveChatMessage` y `addMessage` en `ChatBoxPatches.cs`**
  - **Prioridad:** Crítica
  - **Análisis Técnico:** Postfixes sobre `ChatBox.receiveChatMessage` y `ChatBox.addMessage` capturando todos los mensajes en `ChatViewState` con sello horario opcional `[HH:mm]`.
  - **Criterios de Aceptación:** Implementado en `ChatBoxPatches.cs`.

- [x] **TASK-103: Renderizador de viewport acotado, recorte vertical y barra de scroll (`ChatScrollRenderer.cs`)**
  - **Prioridad:** Crítica
  - **Análisis Técnico:** Sustituye el bucle de `ChatBox.draw` mediante `ScissorRectangle`, altura máxima `MaxVisibleHeight = 280px`, modo cerrado `MaxClosedHeight` con desvanecimiento y scrollbar nativa de `Game1.mouseCursors` con pulgar arrastrable y flechas.
  - **Criterios de Aceptación:** Implementado en `ChatScrollRenderer.cs`.

- [x] **TASK-104: Atajos de teclado de scroll e indicador `"↓ N mensajes nuevos"`**
  - **Prioridad:** Alta
  - **Análisis Técnico:** `ChatBoxPatches.Prefix_ReceiveKeyPress` soporta `PageUp`, `PageDown`, `Ctrl+Home`, `Ctrl+End` manteniendo `Up`/`Down` intactos; `ChatScrollRenderer.DrawNewMessagesPill` dibuja la píldora clicable.
  - **Criterios de Aceptación:** Implementado en `ChatBoxPatches.cs` y `ChatScrollRenderer.cs`.

---

### Iteración 2: Sistema de Pestañas, Ruteo Contextual y Clasificador
> **Objetivo:** Separar la conversación en canales legibles, permitir escribir en el canal activo sin prefijos y notificar actividad en pestañas en segundo plano.

- [x] **TASK-201: Modelo puro de pestañas y contadores (`ChatTabState.cs`, `ChatChannelType.cs`)**
  - **Prioridad:** Crítica
  - **Criterios de Aceptación:** Implementado y cubierto por `ChatViewStateTests.cs`.

- [x] **TASK-202: Renderizado e interacción de barra de pestañas (`ChatTabBarRenderer.cs`)**
  - **Prioridad:** Crítica
  - **Criterios de Aceptación:** Implementado con badges `(N)`, clic izquierdo, `Tab`/`Shift+Tab` e `i18n`.

- [x] **TASK-203: Ruteo contextual de entrada (`ChatRouterLogic.cs` + parche en `textBoxEnter`)**
  - **Prioridad:** Alta
  - **Criterios de Aceptación:** Implementado en `ChatRouterLogic.ResolveOutgoing`, `ChatBoxPatches.Prefix_TextBoxEnter` y `ChatScrollRenderer.DrawActiveChannelTag`, cubierto por `ChatRouterLogicTests.cs`.

- [x] **TASK-204: Clasificador de mensajes de sistema y vanilla (`SystemMessageClassifier.cs`)**
  - **Prioridad:** Alta
  - **Criterios de Aceptación:** Implementado y cubierto por `SystemMessageClassifierTests.cs`.

- [x] **TASK-205: Avisos críticos transversales (*Cross-Tab Pinned Toast*)**
  - **Prioridad:** Media
  - **Criterios de Aceptación:** Implementado en `ChatViewState.ActiveCriticalToast` y `ChatScrollRenderer.DrawCriticalToast`, cubierto por `ChatViewStateTests.cs`.

---

### Iteración 3: Puente con Canal de Discord y Sanitización
> **Objetivo:** Preparar la comunicación bidireccional segura con Discord sin exponer tokens en los clientes ni permitir inyecciones sobre `ChatMessage`.

- [x] **TASK-301: Sanitizador de mensajes externos (`ChatSanitizer.cs`)**
  - **Prioridad:** Crítica
  - **Criterios de Aceptación:** Implementado y cubierto por `ChatSanitizerTests.cs`.

- [x] **TASK-302: Canal y comando `/d` (`/discord`) y transporte SMAPI de canal Discord**
  - **Prioridad:** Alta
  - **Criterios de Aceptación:** Implementado en `ChatTransportModule.cs` (`HandleDiscordCommand` + `OnModMessageReceived`).

- [x] **TASK-303: Puente autónomo de Discord en Host (`DiscordBridgeModule.cs`)**
  - **Prioridad:** Media
  - **Descripción:** Cola asíncrona en segundo plano hacia `DiscordWebhookUrl` (con intervalo de envío de 250 ms y prevención de eco) y listener HTTP loopback opcional (`127.0.0.1:DiscordInboundPort`) autenticado por Bearer Token cuando `Pelican 4Ever` no está activo. Reenvía exclusivamente el canal `Discord` (`ADR-0004`).
  - **Criterios de Aceptación:** Implementado en `DiscordBridgeModule.cs` sin bloquear `UpdateTicked` ni exponer secretos a clientes.

- [x] **TASK-304: Prevención de bucles de eco y filtrado de privacidad**
  - **Prioridad:** Crítica
  - **Criterios de Aceptación:** `ChatRouterLogic.ShouldForwardToDiscord` bloquea `Global`, `Local`, `Whisper`, `System` y entradas con `Origin != ExternalOrigin.None` (`ADR-0004`), cubierto por `ChatRouterLogicTests.cs`.

---

### Iteración 4: Integración Plena con `Pelican 4Ever`
> **Objetivo:** Lograr acoplamiento transparente, sin colisiones y enriquecido entre `PelicanChat` y `Pelican4Ever.SVRTMP`.

- [x] **TASK-401: Puente de integración y detección en vivo (`Pelican4EverBridge.cs`, `IPelican4EverChatApi.cs`)**
  - **Prioridad:** Crítica
  - **Criterios de Aceptación:** Implementado con `Helper.ModRegistry.GetApi<IPelican4EverChatApi>("Pelican4Ever.SVRTMP")` sin referencia DLL ni duplicidad de comandos en `ChatTransportModule.cs`.

- [ ] **TASK-402: Sincronización de marcas horarias `[HH:mm]` del reloj 1:1**
  - **Prioridad:** Media
  - **Estado:** *Parcial* — `Pelican4EverBridge.GetTimestampText()` consulta `api.GetServerTimeFormatted()` (y hace fallback a `Game1.timeOfDay` / hora local); queda pendiente exponer `IsNightHold()` en `IPelican4EverChatApi` si se desea distinguir visualmente el tramo `2550` / madrugada.

- [x] **TASK-403: Clasificación de eventos de módulos de `Pelican 4Ever`**
  - **Prioridad:** Alta
  - **Criterios de Aceptación:** Implementado en `SystemMessageClassifier.cs` y cubierto por `SystemMessageClassifierTests.cs`.

- [x] **TASK-404: Compatibilidad con `Pelican4Ever.Enabled = false` (`ModToggleModule`)**
  - **Prioridad:** Media
  - **Criterios de Aceptación:** `Pelican4EverBridge.IsPelican4EverActive` consulta `api.IsModEnabled()` en caliente y `ChatTransportModule.EnsureCommandsRegistered()` reevalúa comandos autónomos en `SaveLoaded` sin duplicar los ya registrados.

- [ ] **TASK-405: Documentación de despliegue con `RequiredClientMods` y pruebas de integración**
  - **Prioridad:** Media
  - **Estado:** *Parcial* — Documentado en `README.md` (`RequiredClientMods` con `"Pelican4Ever.Chat"`); pendiente registrar evidencia de arranque conjunto en vivo en `docs/verifications/`.

---

### Iteración 5: Normalización al Project Brain (`EST-001` v1.2)
> **Objetivo:** Mantener la memoria operativa, ADRs, guardas deterministas y contratos sincronizados con `MCP Pelican`.

- [x] **BRAIN-001: Normalización estructural y guardas automáticas (`EST-001` v1.2)**
  - **Descripción:** Alinear ADRs (`ADR-0004` para relevo exclusivo del canal `Discord` por **Yuki** y contrato real `IPelican4EverChatApi`), actualizar `architecture.md`, `design.md`, `GSS.md`, `README.md`, `AGENTS.md` y añadir `scripts/verify_brain.py` + `BrainDocsContractTests.cs`.
  - **Criterios de Aceptación:** `python3 scripts/verify_brain.py --check`, `check_harmony_patches.py --strict`, `dotnet build` y `dotnet test` (incluyendo `BrainDocsContractTests` e `I18nParityTests`) pasan con `0` errores y `0` advertencias.

---

### Tareas de Puente en el Repositorio `Pelican 4Ever` (`Stardew Valley MORPG`)
> **Nota:** Estas 3 tareas viven en el repositorio hermano `Pelican4Ever` y ya figuran implementadas para habilitar la comunicación oficial de API y la retransmisión hacia los clientes.

- [x] **P4E-CHAT-01: Ampliar `PelicanApi.cs` con contrato de chat y reloj**
  - **Archivo destino (`Pelican4Ever`):** `Pelican4Ever/PelicanApi.cs`
  - **Estado:** Implementado (`IsModEnabled()`, `GetServerTimeFormatted()`, `PublishChatEvent(channel, senderName, message)`).

- [x] **P4E-CHAT-02: Corregir retransmisión Host $\rightarrow$ Clientes en `AdminApiModule.BroadcastToAllPlayers`**
  - **Archivo destino (`Pelican4Ever`):** `Pelican4Ever/Modules/AdminApiModule.cs`
  - **Estado:** Implementado (`ChatMessages.ServerBroadcast` hacia `Pelican4Ever.SVRTMP` y `Pelican4Ever.Chat` + `Game1.chatBox?.addMessage(...)`).

- [x] **P4E-CHAT-03: Añadir `POST /api/v1/host/chat` y `HostEventTypes.ChatMessage`**
  - **Archivos destino (`Pelican4Ever`):** `Pelican4Ever.Core/Admin/HostEventLog.cs`, `Pelican4Ever.Core/Admin/HostApiContract.cs`, `Pelican4Ever/Modules/AdminApiModule.cs`, `docs/host-api.md`
  - **Estado:** Implementado (`HostEventTypes.ChatMessage = "chat.message"`, `POST /api/v1/host/chat` documentado y enrutado hacia `ServerBroadcast` / `ChatBox`).
