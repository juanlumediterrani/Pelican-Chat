# PelicanChat — Especificación General del Sistema (GSS)

> **Nombre del producto:** *PelicanChat* (`UniqueID: Pelican4Ever.Chat`)
> **Documento:** General System Specification (GSS). Qué es el producto, para quién, cómo se comporta y qué requisitos normativos cumple.
> El detalle técnico de implementación vive en [`architecture.md`](architecture.md), las decisiones de arquitectura en [`docs/adr/`](docs/adr/README.md) y la hoja de ruta en [`backlog.md`](backlog.md).
> **Versión:** 0.1 · **Fecha:** 2026-11-15

---

## 1. Propósito y Alcance

**PelicanChat** es un mod independiente para **Stardew Valley 1.6.15+** (sobre **SMAPI 4.1+**) que transforma el `ChatBox` nativo en una vista de conversación persistente con **historial desplazable (scroll)**, **pestañas por canal**, **ruteo contextual de entrada** y **puente bidireccional con Discord**.

### 1.1 Principio de Dualidad: Autónomo e Integrado

1. **Modo Autónomo (Vanilla / Co-op estándar):**
   - Funciona sin requerir **Pelican 4Ever** ni otros mods de interfaz.
   - Proporciona por sí mismo: buffer ampliado con scroll (rueda del ratón, scrollbar visual y `PageUp`/`PageDown`), 6 pestañas de canal (`Todo`, `Local`, `Global`, `Discord`, `Susurros`, `Eventos`), chat de proximidad propio (`/l` o `/local` con radio de `30` tiles en la misma ubicación), susurros (`/w`), y puente opcional con un canal de Discord.
2. **Modo Integrado (`Pelican 4Ever` detectado):**
   - Cuando el mod `Pelican4Ever.SVRTMP` está instalado y activo en la sesión (consultado mediante `Helper.ModRegistry.GetApi<IPelican4EverChatApi>("Pelican4Ever.SVRTMP")`), **PelicanChat** se acopla automáticamente:
     - Se sincroniza con el reloj 1:1 del servidor (`06:00–01:50` y estado `NightHold` en `2550`) para estampar la hora real/de juego `[HH:mm]` en cada entrada.
     - Coopera con `ChatChannelModule` (`Pelican4Ever`) para evitar duplicar registros en `ChatCommands` (`/l`, `/g`, `/w`) y compartir los paquetes de proximidad `LocalChat`.
     - Clasifica automáticamente los mensajes generados por los módulos de **Pelican 4Ever** (*Rollover* de las `02:00 AM`, avisos de *Festivales*, *Mercado Vecinal* y *Pedidos por Correo*) en la pestaña `Eventos`.
     - Utiliza la API REST de anfitrión (`/api/v1/host/chat` y `HostEventLog` con `chat.message` en `GET /api/v1/host/events?since=N`) para conectar el juego con **Discord**, el anfitrión agente (**Dr4co / Stardew Voyager**) y el bot guardián (**Putre / Hermes**).

### 1.2 Fuera de Alcance

- Almacenar secretos o tokens de bot de Discord (`DISCORD_TOKEN`) en el cliente del juego o en `config.json` de los *farmhands*.
- Sustituir por completo `Game1.chatBox` por un objeto ajeno a `StardewValley.Menus.ChatBox` (rompería la compatibilidad con mods y atajos nativos del motor).
- Reproducir audio, imágenes o adjuntos binarios de Discord dentro del `ChatBox` (los adjuntos se representan de forma textual limpia o se omiten con aviso).

### 1.3 Definiciones y Glosario

| Término | Definición |
| :--- | :--- |
| **Vista Deslizante (*Scroll Viewport*)** | Ventana visual de altura máxima acotada (`MaxVisibleHeight = 280px`) sobre el historial circular de la pestaña activa, controlada por `ScrollOffset`. |
| **Scroll Lock** | Comportamiento que congela la posición actual de lectura cuando `ScrollOffset > 0`, evitando saltos automáticos al llegar mensajes nuevos. |
| **Canal de Salida Implícito** | Canal al que se envía un mensaje sin prefijo `/` al pulsar `Enter`, determinado automáticamente por la pestaña activa en ese momento. |
| **Insignia de No Leídos (*Unread Badge*)** | Contador visual `(N)` o parpadeo sutil sobre una pestaña inactiva cuando recibe mensajes nuevos sin que el jugador la tenga enfocada. |
| **Aviso Transversal Crítico (*Cross-Tab Toast*)** | Mensaje de sistema prioritario (p. ej. cuenta atrás de *Rollover* o `[Reinicio Controlado]`) que se muestra temporalmente sobre el chat aunque el jugador filtre otra pestaña. |
| **Puente de Eventos (*EventLog Bridge*)** | Mecanismo bidireccional que expone mensajes de juego salientes en `HostEventLog` (`chat.message`) e ingiere mensajes externos entrantes vía `POST /api/v1/host/chat`. |

---

## 2. Principios de Diseño

1. **Cero fricción al conversar:** Quien está hablando en la plaza no debe teclear `/l` delante de cada frase. Si selecciona la pestaña `Local`, escribe texto normal y sale por proximidad; si cambia a `Discord`, escribe normal y cruza al canal de Discord.
2. **Lectura sin tirones (*Scroll Lock*):** Leer una conversación pasada mientras otros jugadores hablan en `Global` o `Discord` no debe provocar que el scroll salte hacia abajo con cada mensaje nuevo.
3. **Estética 100% nativa de Stardew Valley:** Todo el cromo (pestañas, fondos, barra de scroll, flechas y fuentes) usa `Game1.mouseCursors`, `LooseSprites\chatBox`, `LooseSprites\emojis` y `Game1.smallFont`, respetando la escala de UI (`Game1.options.uiScale`) y la legibilidad.
4. **Cero secretos en clientes:** Ningún jugador conecta directamente su cliente a la Gateway API de Discord. Toda comunicación con Discord pasa por el Host (o puente externo autorizado).
5. **Degradación elegante (*Graceful Degradation*):** Si se juega en solitario, en co-op vanilla o con `Pelican 4Ever` desactivado (`Enabled = false`), el mod sigue operando al 100% de sus capacidades autónomas sin errores.

---

## 3. Perfiles de Usuario

| Perfil | Necesidad Principal | Fricción Actual en Vanilla | Respuesta de PelicanChat |
| :--- | :--- | :--- | :--- |
| **Vecino de Pelican 4Ever** | Seguir charlas locales, globales, avisos de festivales y el chat con Discord en sesiones de 30–90 min. | El historial se borra a los 10 mensajes, sin scroll y todo mezclado en un bloque. | Historial de 200 mensajes/canal, scroll suave, pestañas dedicadas y sellos `[HH:mm]` del reloj 1:1. |
| **Jugador Co-op Vanilla** | Mejorar el chat de partidas cooperativas estándar sin instalar el mod completo de servidor persistente. | Sin pestañas, sin proximidad y sin poder releer mensajes anteriores. | Funciona de forma autónoma con todas las mejoras de UI, scroll, proximidad y Discord opcional. |
| **Participante desde Discord** | Saludar o coordinarse desde *La Taberna* (Discord) con quienes están conectados en el valle. | Desconexión total entre Discord y la ventana del juego (salvo anuncios puntuales del bot). | Canal puenteado bidireccional con prefijo `[Discord]` claro y sanitización segura. |
| **Anfitrión Agente / Guardián (`Dr4co` / `Putre`)** | Emitir avisos operativos, leer mensajes de comunidad desde `HostEventLog` y garantizar llegada a todos los *farmhands*. | `BroadcastToAllPlayers` en Host solo pintaba localmente sin llegar a clientes por red. | Retransmisión real Host $\rightarrow$ Clientes vía SMAPI e integración nativa con `/api/v1/host/chat`. |

---

## 4. Requisitos Funcionales (`RF`)

### 4.1 Historial Ampliado y Scroll (`RF-01`..`RF-10`)

| ID | Requisito | Prioridad |
| :--- | :--- | :--- |
| `RF-01` | Mantener un buffer circular configurable en memoria por cada canal (por defecto `200` mensajes por pestaña) en lugar del límite de `10` de vanilla. | Alta |
| `RF-02` | Acotar la altura máxima visible del cuadro de mensajes (`MaxVisibleHeight = 280px` por defecto, configurable) para no invadir el centro de la pantalla. | Alta |
| `RF-03` | Soportar desplazamiento vertical mediante la **rueda del ratón** cuando el `ChatBox` está activo y `!choosingEmoji`, sin cambiar el ítem seleccionado de la barra de herramientas. | Alta |
| `RF-04` | Soportar desplazamiento mediante teclas **`PageUp`**, **`PageDown`**, **`Ctrl+Home`** (inicio del historial) y **`Ctrl+End`** (último mensaje), conservando `Up`/`Down` intactos para `cheatHistory`. | Alta |
| `RF-05` | Renderizar una **barra de scroll interactiva** en el borde derecho del panel (usando los sprites nativos de `Game1.mouseCursors`) con flechas arriba/abajo, canal y pulgar arrastrable con ratón. | Media |
| `RF-06` | Implementar **Scroll Lock**: si `ScrollOffset > 0`, los mensajes entrantes incrementan el desplazamiento relativo para mantener fija la línea visible actual. | Alta |
| `RF-07` | Mostrar una píldora/banda inferior clicable `"↓ N mensajes nuevos"` cuando el jugador está desplazado arriba y entran mensajes en la pestaña activa; al hacer clic, salta al fondo (`ScrollOffset = 0`). | Media |
| `RF-08` | Cuando el `ChatBox` está cerrado (`!chatBox.Selected`), mostrar únicamente los mensajes recientes cuyo `timeLeftToDisplay` siga vigente, respetando el desvanecimiento (`alpha`) y el tope máximo de altura configurada. | Alta |
| `RF-09` | Al abrir el chat (`chatBox.activate()`), restaurar la visibilidad completa del historial de la pestaña activa en su `ScrollOffset` actual (por defecto abajo del todo). | Alta |
| `RF-10` | Limpiar cualquier recorte de texto (*scissor rect* o cálculo exacto de líneas visibles) para que ningún mensaje se dibuje fuera de los límites verticales del panel. | Alta |

### 4.2 Sistema de Pestañas y Ruteo Contextual (`RF-11`..`RF-22`)

| ID | Requisito | Prioridad |
| :--- | :--- | :--- |
| `RF-11` | Mostrar una barra horizontal de pestañas encima del cuadro de mensajes cuando el `ChatBox` está activo: `Todo`, `Local`, `Global`, `Discord`, `Susurros` y `Eventos` (pestañas configurables). | Alta |
| `RF-12` | Ampliar la comprobación de límites (`isWithinBounds`) para incluir la barra de pestañas y el botón de salto al final, evitando que hacer clic en ellas dispare `clickAway()` y cierre el chat. | Alta |
| `RF-13` | Permitir cambiar de pestaña con **clic izquierdo** o con **`Tab` / `Shift+Tab`** mientras el `ChatBox` está abierto. | Alta |
| `RF-14` | Aplicar **Canal de Salida Implícito** al pulsar `Enter` con texto sin prefijo `/`: `Todo` $\rightarrow$ Global, `Local` $\rightarrow$ Proximidad (`/l`), `Global` $\rightarrow$ Global, `Discord` $\rightarrow$ Discord (`/d`), `Susurros` $\rightarrow$ Último interlocutor (`/r`). | Alta |
| `RF-15` | Mantener explícitamente los comandos `/g`, `/l` (`/local`), `/w` (`/whisper`, `/dm`, `/pm`, `/message`), `/r` (`/reply`) y añadir `/d` (`/discord`) desde cualquier pestaña. | Alta |
| `RF-16` | Indicar claramente en el campo de entrada (`ChatTextBox`) el canal de destino activo antes de escribir o como etiqueta lateral (p. ej. `[Local]`, `[Discord]`, `[Para: Nombre]`). | Media |
| `RF-17` | En la pestaña `Eventos` (solo lectura para texto libre), permitir igualmente la ejecución de comandos `/` (como `/evento`, `/residencias`, `/help`) y mostrar ayuda si se escribe texto plano. | Media |
| `RF-18` | Calcular y mostrar insignias de mensajes no leídos (`UnreadCount`) en cada pestaña inactiva que reciba actividad nueva; al seleccionar la pestaña, reiniciar su contador a `0`. | Media |
| `RF-19` | Permitir anclar avisos críticos transversales (*Cross-Tab Toast*) como reinicios programados o cuentas atrás del *Rollover* para que sean visibles temporalmente en cualquier pestaña. | Alta |
| `RF-20` | Soportar colores semánticos diferenciados por canal (Verde claro `#82E682` para `Local`, Blanco/Crema para `Global`, Blurple `#7289DA` para `Discord`, Cian `#00CED1` para `Susurros`, Dorado `#FFD700` para `Eventos`). | Alta |
| `RF-21` | Permitir silenciar o colapsar los mensajes de `Discord` en la pestaña `Todo` mediante configuración (`IncludeDiscordInAllTab`) para jugadores que prefieran separarlo. | Baja |
| `RF-22` | Recordar la última pestaña seleccionada durante la sesión de juego. | Baja |

### 4.3 Conexión con Discord y Sanitización (`RF-23`..`RF-32`)

| ID | Requisito | Prioridad |
| :--- | :--- | :--- |
| `RF-23` | Definir un modelo unificado de mensaje externo (`ExternalChatMessage`) con origen (`Discord`, `HostAgent`), autor, canal, marca temporal y cuerpo. | Alta |
| `RF-24` | **Camino Integrado (con `Pelican 4Ever`):** El Host registra los mensajes salientes de `Global` y `Discord` en `AdminApiModule.EventLog` con tipo `chat.message` para que el puente externo (`hermes-putre-discord` o daemon) los publique en Discord sin exponer tokens en el juego. | Alta |
| `RF-25` | **Camino Integrado (entrada desde Discord):** El Host expone `POST /api/v1/host/chat` en `AdminApiModule` para recibir mensajes validados desde Discord/Putre y retransmitirlos a todos los clientes conectados. | Alta |
| `RF-26` | **Camino Autónomo (sin `Pelican 4Ever`):** Soportar configuración opcional en el Host de un `DiscordWebhookUrl` (solo salida asíncrona no bloqueante) o puente HTTP loopback local cuando se ejecuta sin `Pelican 4Ever`. | Media |
| `RF-27` | Sanitizar obligatoriamente todo mensaje entrante de Discord en `ChatSanitizer` antes de pasarlo a `ChatBox` / `ChatMessage.parseMessageForEmoji`. | Alta |
| `RF-28` | `ChatSanitizer` debe neutralizar etiquetas `[color]` o `[índice]` arbitrarias procedentes de texto externo, truncar a `MaxInboundMessageLength` (p. ej. `280` caracteres), normalizar saltos de línea y reemplazar caracteres/emojis fuera de `Fonts\SmallFont` por equivalentes ASCII seguros o `?` controlado. | Alta |
| `RF-29` | Formatear los mensajes procedentes de Discord con el prefijo identificable `[Discord] <Autor>: <Texto>` y su color institucional (`#7289DA` o variante legible sobre el fondo beige de Stardew). | Alta |
| `RF-30` | Garantizar que los canales privados o espaciales (`Local` y `Susurros`) **nunca** se reenvían a Discord ni se exponen en `HostEventLog`. | Alta |
| `RF-31` | Aplicar *rate limiting* configurable en el envío hacia/desde Discord para evitar inundación del `ChatBox` o bloqueos de webhook. | Media |
| `RF-32` | Evitar bucles de eco (*echo loops*): un mensaje inyectado desde Discord hacia el juego no debe volver a emitirse como `chat.message` saliente hacia Discord. | Alta |

### 4.4 Integración Plena con `Pelican 4Ever` (`RF-33`..`RF-45`)

| ID | Requisito | Prioridad |
| :--- | :--- | :--- |
| `RF-33` | Detectar la presencia y estado activo de `Pelican4Ever.SVRTMP` en `GameLaunched` mediante `Helper.ModRegistry` y su API pública. | Alta |
| `RF-34` | Si `Pelican 4Ever` está activo, delegar o sincronizar con `ChatChannelModule` la gestión de `/l`, `/g` y `/w` sin registrar comandos duplicados ni romper los ya existentes. | Alta |
| `RF-35` | Si `Pelican 4Ever` **no** está instalado o está desactivado (`Enabled = false`), `PelicanChat` asume automáticamente la gestión autónoma de `/l`, `/g`, `/w` y `/d` y del bus SMAPI propio. | Alta |
| `RF-36` | Consultar la hora del servidor en `Pelican 4Ever` (o la hora local/juego en modo autónomo) para prefijar opcionalmente cada entrada del historial con `[HH:mm]`. | Media |
| `RF-37` | Clasificar automáticamente en la pestaña `Eventos` los avisos nativos y de módulos de **Pelican 4Ever**: prefijos `[Festival]`, `[Ceremonia]`, `[Reinicio Controlado]`, `[Mercado]`, `[Correo]`, `[Rollover]`, `[Madrugada]` y notificaciones amarillas (`chatKind == 2`) / doradas (`255, 215, 0`). | Alta |
| `RF-38` | Asegurar que `BroadcastToAllPlayers` y los anuncios de `AdminApiModule` en `Pelican 4Ever` lleguen a todos los *farmhands* conectados (no solo al proceso headless del Host). | Alta |
| `RF-39` | Respetar la activación/desactivación dinámica de `Pelican 4Ever` (`ModToggleModule`), permitiendo que `PelicanChat` siga funcionando en modo autónomo si `Pelican 4Ever` pasa a `Enabled = false`. | Media |
| `RF-40` | Respetar `RequiredClientMods` en entornos **Pelican 4Ever**: documentar cómo el administrador puede incluir `Pelican4Ever.Chat` como obligatorio u opcional según desee. | Media |

---

## 5. Requisitos No Funcionales (`RNF`)

| ID | Requisito | Métrica / Criterio |
| :--- | :--- | :--- |
| `RNF-01` | **Separación de Lógica Pura** | Toda la gestión de buffers, scroll, pestañas, clasificación, ruteo y sanitización reside en `PelicanChat.Core` (`net6.0`) sin ninguna referencia a `StardewValley`, `SMAPI`, `MonoGame` ni `Harmony`. |
| `RNF-02` | **Cobertura de Tests** | Toda la lógica de `PelicanChat.Core` se verifica mediante tests unitarios `xUnit` en `PelicanChat.Tests` (`net8.0`) ejecutables con `dotnet test` en < 2 segundos. |
| `RNF-03` | **Calidad de Compilación** | `TreatWarningsAsErrors = true` y `Nullable = enable` en todos los proyectos de la solución (`0` advertencias). |
| `RNF-04` | **Rendimiento de Renderizado** | El dibujo del `ChatBox` mejorado no realiza llamadas HTTP ni asignaciones pesadas por fotograma (`draw()` opera sobre caché de layout recalculada solo al cambiar mensajes, ancho o idioma). |
| `RNF-05` | **Reversibilidad de Parches** | Todos los parches sobre `StardewValley.Menus.ChatBox` se registran con `HarmonyPatchToggle` para poder activarse/desactivarse limpiamente sin reiniciar el juego. |
| `RNF-06` | **Internacionalización (`i18n`)** | Soporte completo desde el día 1 para inglés (`i18n/default.json`), español (`i18n/es.json`) y chino simplificado (`i18n/zh-CN.json`), alineado con `Pelican 4Ever`. |
