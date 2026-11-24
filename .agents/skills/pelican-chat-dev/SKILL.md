---
name: pelican-chat-dev
description: >-
  Guía de desarrollo de PelicanChat: separación entre PelicanChat.Core
  (puro net6.0) y parches Harmony reversibles sobre ChatBox, sanitización de
  mensajes Discord e integración opcional GetApi con Pelican4Ever y Yuki.
---

# Desarrollo Técnico — PelicanChat

Playbook del mod `Pelican4Ever.Chat` para Stardew Valley 1.6.15+ / SMAPI 4.1+.

---

## 1. Arquitectura y Fronteras

1. **`PelicanChat.Core/` (`net6.0`, cero dependencias del juego):**
   - Concentra canales (`ChatRouterLogic`, `SystemMessageClassifier`), buffers de viewport (`ChatHistoryBuffer`, `ChatViewState`, `ChatTabState`), configuración (`ChatConfig`) y sanitización Discord (`ChatSanitizer`, `ExternalChatContract`).
   - Toda lógica nueva de formateo, filtrado, paginación o sanitización debe vivir aquí y cubrirse en `PelicanChat.Tests/`.
2. **`PelicanChat/` (adaptador SMAPI + Harmony):**
   - `Patches/ChatBoxPatches.cs` envuelve `Game1.chatBox` usando `HarmonyPatchToggle.cs` para garantizar reversibilidad total al deshabilitar `ChatConfig.Enabled`.
   - `Integration/Pelican4EverBridge.cs` resuelve `Pelican4Ever.SVRTMP` exclusivamente por `ModRegistry.GetApi<IPelican4EverChatApi>` sin referencias de ensamblado directo.

---

## 2. Seguridad en Puente Discord y Render

- **Sanitización previa:** ningún mensaje procedente de Discord o API externa entra a `ChatMessage` sin pasar por `ChatSanitizer` (bloquea etiquetas de color/sprite arbitrarias e inyecciones de comandos).
- **Anti-eco y reloj:** al estar conectado a `Pelican4Ever`, respeta `ServerClock` para sellos `[HH:mm]` y evita reemitir eventos originados por el bus `/api/v1/host/chat` consumido por **Yuki** (`DEC-0004`).
- **Sin secretos en el cliente:** el token y webhook de Discord nunca se exponen a clientes farmhand; la validación falla cerrado ante configuración inválida (`ChatConfig.Validate()`).

---

## 3. Verificación Rápida

Activa también la skill compartida `smapi-harmony-pelican-bridge` (enlazada vía `.agents/skills.json` desde `MCP Pelican`) cuando modifiques `Patches/ChatBoxPatches.cs` o `HarmonyPatchToggle.cs`, y ejecuta:

```bash
python3 "../MCP Pelican/skills/smapi-harmony-pelican-bridge/scripts/check_harmony_patches.py" . --strict
dotnet test PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
```
