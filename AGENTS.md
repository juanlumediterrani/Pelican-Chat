# `AGENTS.md` — Índice Operativo del Brain (*PelicanChat*)

> **Punto de entrada obligatorio (`EST-001` v1.2 de Niu D'Ocells).**
> Lee estas reglas antes de abrir archivos en `docs/`, enruta desde la tabla Código → Memoria y ejecuta el cierre *write-back* antes de terminar.

---

## 1. Reglas de Contexto y Precedencia

1. **Idioma único normativo:** el **español** (ficheros `.md` sin sufijo de idioma) es la fuente única del Project Brain.
2. **Rutas prohibidas salvo petición literal:**
   - `docs/archive/**`, `docs/verifications/**`, `**/*.en.md`, `**/*.zh-CN.md`.
   - `bin/`, `obj/`, `data/`, `local.props`, `config.json`.
3. **Precedencia ante conflicto:**
   1. Código fuente y tests unitarios: `PelicanChat.Core/**`, `PelicanChat/**`, `PelicanChat.Tests/**`.
   2. ADRs `Aceptados`: [`docs/adr/README.md`](docs/adr/README.md).
   3. Especificaciones vigentes: [`architecture.md`](architecture.md), [`design.md`](design.md), [`GSS.md`](GSS.md).
   4. [`backlog.md`](backlog.md) local *(traza técnica de desarrollo; no sustituye a ERPNext `DEC-0001`)*.
4. **Numeración de ADRs:** `0001..0004` están `Aceptados`; cualquier nueva decisión toma **`0005+`**.
5. **Ecosistema Nivel 2 (`MCP Pelican`):** usa `MCP Pelican` (`../MCP Pelican`, `repo_id="pelican-chat"`) para consultar el bus `/api/v1/host/chat` en `Pelican4Ever` (`docs/host-api.md`) o la integración con **Yuki** (`DEC-0004`).

---

## 2. Invariantes Críticos y Empresa (`Niu D'Ocells`)

- **Dualidad autónomo + puente SMAPI:** `PelicanChat` funciona 100 % sin `Pelican4Ever` y, cuando `Pelican4Ever.SVRTMP` está presente, se acopla mediante `Helper.ModRegistry.GetApi` (`Integration/Pelican4EverBridge.cs`, `IPelican4EverChatApi.cs`) **sin referenciar ensamblados DLL** de `Pelican4Ever`.
- **Núcleo puro y parches reversibles:** `PelicanChat.Core` (`net6.0`) no referencia Stardew/SMAPI/Harmony. Todo parche sobre `ChatBox` (`Patches/ChatBoxPatches.cs`) pasa por `HarmonyPatchToggle` para restauración limpa al desactivar o recargar.
- **Seguridad de mensajes y secretos:** `ChatSanitizer.cs` neutraliza etiquetas `[color]`/`[emoji]` en mensajes entrantes; jamás se exponen webhooks/tokens en el cliente ni en el repositorio.
- **Compilación y tests seguros:**
  ```bash
  python3 scripts/verify_brain.py --check
  dotnet build PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
  dotnet test PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
  ```
- **Gobernanza corporativa (`Niu D'Ocells`):** ERPNext es la fuente viva de tareas de empresa (`DEC-0001`, `PROJ-0005`), los zips de release se depositan exclusivamente en `~/Documentos/Niu D'Ocells/produccio/` (`DEC-0002`), y **Yuki** sustituye a Putre como operador runtime/Discord en AWS (`DEC-0004`).

---

## 3. Enrutamiento Condicional (Código → Memoria)

| Si vas a tocar... | Lectura obligatoria | Qué regula |
| --- | --- | --- |
| `PelicanChat/ModEntry.cs`, `PelicanChat/Integration/*`, `PelicanChat/Framework/IPelican4EverChatApi.cs`, `PelicanChat/manifest.json` | [`ADR-0001`](docs/adr/0001-standalone-mod-and-pelican4ever-bridge.md), [`architecture.md`](architecture.md) | Dualidad autónomo/integrado y puente `ModRegistry.GetApi` |
| `PelicanChat/Patches/*`, `PelicanChat/UI/*`, `PelicanChat/Modules/ChatViewModule.cs`, `PelicanChat/Framework/HarmonyPatchToggle.cs`, `PelicanChat.Core/Viewport/*` | [`ADR-0002`](docs/adr/0002-chatbox-harmony-viewport-and-tabs.md), [`GSS.md`](GSS.md) | Parches `ChatBox`, viewport con scroll, pestañas y `HarmonyPatchToggle` |
| `PelicanChat/Modules/{DiscordBridgeModule,ChatTransportModule}.cs`, `PelicanChat/Models/ChatNetworkModels.cs`, `PelicanChat.Core/Discord/*`, `PelicanChat.Core/Channels/*` | [`ADR-0003`](docs/adr/0003-discord-bridge-and-message-sanitization.md), [`ADR-0004`](docs/adr/0004-discord-channel-only-and-yuki-relay.md), [`architecture.md`](architecture.md) | Sanitización anti-inyección, relevo Discord-only (`Yuki`) y ruteo de canales |
| `PelicanChat/i18n/*`, `PelicanChat/Framework/I18n.cs`, `PelicanChat.Core/Config/ChatConfig.cs` | [`design.md`](design.md), [`pelican-chat-dev`](.agents/skills/pelican-chat-dev/SKILL.md) | Validación de configuración e i18n (`default`, `es`, `zh-CN`) |

---

## 4. Skills y Protocolo de Cierre (*Write-Back*)

1. **Skills activas:**
   - `pelican-chat-dev`: compilación segura, parches reversibles sobre `ChatBox`, sanitización y puente `ModRegistry.GetApi`.
   - `brain-cierre-tarea`: checklist de verificación y sincronización documental antes de cerrar/PR.
   - `smapi-harmony-pelican-bridge` (compartida desde `MCP Pelican` vía `.agents/skills.json`): patrones Harmony seguros y auditoría `check_harmony_patches.py --strict`.
2. **Write-back obligatorio:** si modificas `IPelican4EverChatApi`, el contrato `ExternalChatContract` / `/api/v1/host/chat`, o parches Harmony en `ChatBoxPatches.cs`, actualiza [`architecture.md`](architecture.md) y el ADR correspondiente (`0001..0004` o `0005+`) **en el mismo commit** (verifica con `python3 scripts/verify_brain.py --diff`).
