# Registros de Decisiones de Arquitectura (ADR) — *PelicanChat*

Un ADR es una **decisión** y su motivo: no se reescribe cuando el contexto cambia, se sustituye por otro ADR. La tabla es el índice; el detalle está en cada fichero.

| ADR | Decisión | Estado | Fecha |
| --- | --- | --- | --- |
| [0001](0001-standalone-mod-and-pelican4ever-bridge.md) | Mod independiente (`Pelican4Ever.Chat`) con modo autónomo y puente automático hacia `Pelican 4Ever` | Aceptado | 2026-11-15 |
| [0002](0002-chatbox-harmony-viewport-and-tabs.md) | Conservar `Game1.chatBox` (`ChatBox`) con parches Harmony reversibles respaldados por `ChatViewState` en `PelicanChat.Core` | Aceptado | 2026-11-15 |
| [0003](0003-discord-bridge-and-message-sanitization.md) | Cero tokens de Discord en clientes; puente en Host (`/api/v1/host/chat` + `HostEventLog`) y sanitización obligatoria | Aceptado | 2026-11-15 |

## Cómo leerlos

- **Contexto:** por qué la decisión era necesaria.
- **Decisión:** qué se decidió, en sus términos exactos.
- **Consecuencias:** qué queda atado a esa decisión.

## Reglas

1. Un ADR no se edita para cambiar la decisión: se marca como *sustituido* y se escribe otro.
2. Las cifras y contratos citados (tamaños de buffer, alturas, endpoints, UniqueIDs) son **normativos**: el código debe coincidir con ellos.
3. Cada decisión nueva que afecte al contrato de API, al modelo de red o a la integración con `Pelican 4Ever` / Discord merece un ADR.
