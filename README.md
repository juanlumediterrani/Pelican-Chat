# PelicanChat (`Pelican4Ever.Chat`)

Mod independiente para **Stardew Valley 1.6.15+** (**SMAPI 4.1+**) que convierte el `ChatBox` nativo en una vista de chat moderna con **scroll**, **pestañas por canal**, **ruteo contextual** y **puente bidireccional con Discord**, diseñado para funcionar de forma **100% autónoma** en partidas vanilla/cooperativas e **integrarse plenamente** con [**Pelican 4Ever**](../Pelican4Ever/README.md).

---

## Características Principales

- **Historial Ampliado y Scroll Fluido:** Hasta `200` mensajes por canal (frente a `10` de vanilla), altura visual acotada (`280px`), desplazamiento con rueda del ratón, `PageUp`/`PageDown`, *Scroll Lock* sin tirones al leer arriba y scrollbar nativa de Stardew Valley.
- **Sistema de Pestañas:** `Todo` · `Local` (proximidad $\le 30$ tiles) · `Global` · `Discord` · `Susurros` · `Eventos`, navegables por clic o `Tab`/`Shift+Tab`, con insignias `(N)` de mensajes no leídos y **envío contextual implícito** (escribe en `Local` o `Discord` sin anteponer `/l` o `/d`).
- **Listo para Discord:** Canal dedicado `#7289DA`, sanitización estricta contra inyección de corchetes `[color]`/`[emoji]` en `ChatMessage`, prevención de bucles de eco y cero secretos/tokens de bot en los clientes.
- **Dualidad Autónomo + Pelican 4Ever:**
  - **Sin `Pelican 4Ever`:** Provee sus propios comandos `/l`, `/g`, `/w`, `/d`, red de proximidad SMAPI y puente Discord opcional en el Host.
  - **Con `Pelican 4Ever` (`Pelican4Ever.SVRTMP`):** Se acopla vía SMAPI Mod API (`GetApi`) para sincronizar sellos `[HH:mm]` del reloj 1:1, evitar duplicar comandos con `ChatChannelModule`, clasificar avisos de *Rollover*, *Festivales* y *Mercado Vecinal* en `Eventos` y conectarse al bus `/api/v1/host/chat` + `HostEventLog` (`chat.message`) usado por **Yuki** (`DEC-0004`).

---

## Documentación del Proyecto

| Documento | Contenido |
| :--- | :--- |
| [`GSS.md`](GSS.md) | Especificación General del Sistema (principios, perfiles, requisitos `RF-01..40` y `RNF-01..06`). |
| [`design.md`](design.md) | Resumen ejecutivo y mapa documental. |
| [`architecture.md`](architecture.md) | Diseño técnico de la solución `.NET`, verificación sobre `ChatBox`/`ChatMessage`/`Game1` (1.6.15) y contrato con `Pelican 4Ever` y Discord. |
| [`backlog.md`](backlog.md) | Fases, *spikes* de validación, tareas (`TASK-001`..`TASK-405`) y puente en `Pelican4Ever` (`P4E-CHAT-01`..`03`). |
| [`docs/adr/`](docs/adr/README.md) | Registros de Decisiones de Arquitectura (`ADR-0001`..`ADR-0004`). |

---

## Compilación, Tests y Diagnóstico

1. Copiar `local.props.example` a `local.props` (ignorado por Git) y ajustar `<GamePath>` a la instalación local de Stardew Valley.
2. Compilar, probar y validar el Project Brain con .NET SDK 8.0 (`global.json`):
   ```bash
   python3 scripts/verify_brain.py --check
   dotnet build PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
   dotnet test PelicanChat.sln -p:EnableModDeploy=false -p:EnableModZip=false
   ```
3. En consola SMAPI, ejecutar `pchat_status` para consultar modo (`Autónomo` vs `Integrado con Pelican 4Ever`), pestaña activa, conteo por buffer, `ScrollOffset` y estado del puente Discord.

---

## Despliegue en Modpack de `Pelican 4Ever` (`RequiredClientMods`)

- `PelicanChat` (`UniqueID: Pelican4Ever.Chat`) **no declara dependencia dura** hacia `Pelican4Ever.SVRTMP`: puede usarse de forma autónoma o como complemento de clientes en servidores **Pelican 4Ever**.
- En el servidor dedicado de **Pelican 4Ever**, el administrador puede elegir en `config.json`:
  - **Obligatorio en clientes** (recomendado si la comunidad usa el canal `/d` unificado): añadir `"Pelican4Ever.Chat"` dentro de `RequiredClientMods`.
  - **Opcional en clientes**: omitir `"Pelican4Ever.Chat"` de `RequiredClientMods`; quienes lo instalen disfrutarán de pestañas, scroll, sellos `[HH:mm]` del reloj 1:1 y ruteo `/d`, mientras el servidor y **Yuki** siguen operando con normalidad.
