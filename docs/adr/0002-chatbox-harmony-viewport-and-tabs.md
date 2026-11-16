# ADR-0002: Conservar `Game1.chatBox` mediante Parches Harmony Reversibles y `ChatViewState` Puro

* **Estado:** Aceptado
* **Fecha:** 2026-11-15

## Contexto

En Stardew Valley 1.6.15 (`Game1.cs` y `StardewValley.Menus.ChatBox`), múltiples flujos del motor interactúan directamente con el campo público estático `Game1.chatBox`:
- `Game1.IsChatting` lee y escribe `chatBox.isActive()`.
- `Game1.UpdateControlInput` llama directamente a `chatBox.receiveScrollWheelAction`, `chatBox.isWithinBounds`, `chatBox.receiveLeftClick` y `chatBox.clickAway()`.
- `Game1.drawOverlays` dispara los eventos de renderizado SMAPI `RenderingSteps.Overlays_Chatbox` alrededor de `chatBox.update` y `chatBox.draw`.
- Otros mods y los propios módulos de `Pelican 4Ever` invocan `Game1.chatBox?.addMessage(...)` y `Game1.chatBox?.addInfoMessage(...)`.

Reemplazar `Game1.chatBox` por una subclase personalizada o un menú distinto dentro de `Game1.onScreenMenus` rompería la referencia de `onScreenMenus` o provocaría desincronizaciones con mods terceros y con el propio `HarmonyPatchToggle` usado por `ModToggleModule`.

## Decisión

1. **Instancia nativa preservada:** Se mantiene siempre la instancia nativa `Game1.chatBox` (`StardewValley.Menus.ChatBox`) creada por el juego.
2. **Parches Harmony focalizados y reversibles:** Todas las ampliaciones de comportamiento (captura de mensajes, ampliación de `isWithinBounds` para la barra de pestañas, gestión de rueda del ratón cuando `!choosingEmoji`, atajos `Tab`/`PageUp`/`PageDown`, ruteo implícito en `textBoxEnter` y renderizado de viewport acotado) se aplican mediante parches Harmony encapsulados en `HarmonyPatchToggle`.
3. **Estado en `PelicanChat.Core` (`ChatViewState`):** La lista nativa `ChatBox.messages` (truncada a 10 por vanilla) deja de ser la fuente de verdad del historial visual. `ChatViewState` mantiene los buffers circulares por canal (`MaxHistoryPerChannel = 200`), el `ScrollOffset` independiente por pestaña, el *Scroll Lock* ante mensajes nuevos y los contadores `UnreadCount`.
4. **Cota visual estricta:** La caja de mensajes abiertos nunca excede `MaxVisibleHeight = 280px` (más `36px` de la barra superior de pestañas), usando sprites nativos de `Game1.mouseCursors` para la barra de desplazamiento.

## Consecuencias

* **Positivas:** Compatibilidad total con `Game1.chatBox?.addMessage(...)`, selector de emojis (`EmojiMenu`), historial de comandos (`Up`/`Down`), escalado UI y desactivación dinámica en caliente.
* **Negativas:** Dependencia de la firma exacta de los métodos públicos/virtuales de `StardewValley.Menus.ChatBox` (verificada sobre 1.6.15; cualquier actualización mayor del juego requerirá verificar los parches en un *spike*).
