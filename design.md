# Diseño Ejecutivo — *PelicanChat* (`Pelican4Ever.Chat`)

> **Documento resumen.** La especificación funcional completa vive en [`GSS.md`](GSS.md) y la especificación técnica verificada contra el motor en [`architecture.md`](architecture.md). Este fichero mantiene la convención de estructura documental de **Pelican 4Ever**.

---

## 1. Visión General

**PelicanChat** mejora el `ChatBox` de **Stardew Valley 1.6.15+** manteniendo la estética clásica del juego mientras resuelve sus cuatro limitaciones principales en comunidades multijugador:

1. **Historial con Scroll (`Scroll Viewport`):** Sustituye el límite efímero de 10 mensajes por un buffer circular de 200 mensajes por canal, ventana acotada de altura fija, rueda del ratón, `PageUp`/`PageDown`, *Scroll Lock* automático y scrollbar nativa de `Game1.mouseCursors`.
2. **Sistema de Pestañas (`Tabs`):** Organiza la conversación en 6 canales claros (`Todo`, `Local`, `Global`, `Discord`, `Susurros`, `Eventos`), con navegación por clic o `Tab`/`Shift+Tab`, contadores de no leídos y **ruteo contextual implícito** al pulsar `Enter`.
3. **Preparado para Discord:** Aísla por completo los tokens del bot fuera del cliente, sanea cualquier texto externo frente al parser `[color]`/`[emoji]` nativo de `ChatMessage` y define el contrato bidireccional con puentes externos.
4. **Integración Plena y Dualidad con Pelican 4Ever:**
   - **Sin Pelican 4Ever (Autónomo):** Funciona en cualquier partida vanilla o cooperativa estándar con su propio canal de proximidad (`/l`) y puente Discord opcional.
   - **Con Pelican 4Ever (`Pelican4Ever.SVRTMP`):** Se acopla vía SMAPI Mod API (`GetApi`) y mensajería de red para sincronizar marcas horarias `[HH:mm]` del reloj 1:1, evitar colisión de comandos con `ChatChannelModule`, enrutar avisos de *Rollover*, *Festivales*, *Mercado Vecinal* y *Correo* a la pestaña `Eventos` y conectarse al bus `/api/v1/host/chat` + `HostEventLog` junto a **Dr4co (Voyager)** y **Putre (Hermes)**.

---

## 2. Mapa de Documentación del Repositorio

| Documento | Propósito |
| :--- | :--- |
| [`README.md`](README.md) | Introducción, modos de uso y guía de compilación. |
| [`GSS.md`](GSS.md) | Especificación General del Sistema: principios, glosario, perfiles y requisitos funcionales (`RF-01`..`RF-40`) y no funcionales (`RNF-01`..`RNF-06`). |
| [`architecture.md`](architecture.md) | Arquitectura .NET multiproyecto, validación exacta sobre `ChatBox`/`ChatMessage`/`Game1` decompilados (1.6.15) y contrato de integración con `Pelican 4Ever` y Discord. |
| [`backlog.md`](backlog.md) | Hoja de ruta técnica por fases, spikes de motor y criterios de aceptación (`TASK-*` y `P4E-CHAT-*`). |
| [`docs/adr/`](docs/adr/README.md) | Registros de Decisiones de Arquitectura (`ADR-0001` a `ADR-0003`). |
