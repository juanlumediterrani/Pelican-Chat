---
name: brain-cierre-tarea
description: >-
  Checklist de cierre, auditoría Harmony y write-back al Project Brain de
  PelicanChat (EST-001 v1.2 de Niu D'Ocells) antes de dar por completada
  una tarea, commitear o abrir PR.
---

# Cierre de Tarea y *Write-Back* — PelicanChat

Ejecuta estos pasos antes de cerrar cualquier tarea o preparar commit/PR en `PelicanChat`.

---

## 1. Verificación Técnica Obligatoria

```bash
python3 "../MCP Pelican/skills/smapi-harmony-pelican-bridge/scripts/check_harmony_patches.py" . --strict
dotnet build PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
dotnet test PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
```

- Mantén `-p:EnableModDeploy=false -p:EnableModZip=false` en toda validación local o CI.
- Declara exactamente los resultados observados en los comandos anteriores.

---

## 2. Protocolo de *Write-Back* al Brain (`EST-001` v1.2)

| Si modificaste... | Qué actualizar en el mismo commit |
| --- | --- |
| `IPelican4EverChatApi`, `Pelican4EverBridge.cs` o contrato `ExternalChatContract` | [`ADR-0001`](../../../docs/adr/0001-standalone-mod-and-pelican4ever-bridge.md) / [`ADR-0003`](../../../docs/adr/0003-discord-bridge-and-message-sanitization.md) y [`architecture.md`](../../../architecture.md) |
| Parches en `ChatBoxPatches.cs`, viewport o `HarmonyPatchToggle.cs` | [`ADR-0002`](../../../docs/adr/0002-chatbox-harmony-viewport-and-tabs.md) |
| Claves de configuración o i18n (`ChatConfig`, `i18n/*.json`) | [`design.md`](../../../design.md) y [`AGENTS.md`](../../../AGENTS.md) si cambia el mapa |

---

## 3. Normas Corporativas (`Niu D'Ocells`)

1. Todo informe de prueba en vivo va a `docs/verifications/YYYY-MM-DD-<tema>.md`, nunca en la raíz de `docs/`.
2. Sin secretos (`config.json`, `*.token`, `*.secret`) ni paquetes `.zip` dentro del repo; los artefactos de release pertenecen a `~/Documentos/Niu D'Ocells/produccio/` (`DEC-0002`).
3. El seguimiento corporativo vive en ERPNext (`DEC-0001`) y el acoplamiento Discord/AWS usa **Yuki** (`DEC-0004`).
