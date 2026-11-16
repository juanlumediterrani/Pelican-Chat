# ADR-0001: Mod Independiente (`Pelican4Ever.Chat`) con Dualidad Autónomo + Puente a `Pelican 4Ever`

* **Estado:** Aceptado
* **Fecha:** 2026-11-15

## Contexto

La mejora del `ChatBox` de Stardew Valley (scroll, pestañas, ruteo por canal y conexión con Discord) nació de las necesidades de la comunidad **Pelican 4Ever**, pero sus beneficios aplican igualmente a cualquier partida cooperativa estándar de Stardew Valley.

Acoplar la interfaz de chat dentro de `Pelican4Ever.dll` mezclaría lógica de presentación UI con la autoridad del servidor persistente, impediría usar el chat mejorado en partidas vanilla y obligaría a recompilar el núcleo del servidor ante cada ajuste visual. Por otro lado, hacer que el nuevo mod falle si `Pelican4Ever` no está presente limitaría su adopción.

## Decisión

1. **Repositorio y mod separado:** **PelicanChat** vive en su propio repositorio y solución (`PelicanChat.sln`), compilando como mod SMAPI independiente con `UniqueID = "Pelican4Ever.Chat"` y sin dependencia dura (`Dependencies`) en `manifest.json` hacia `Pelican4Ever.SVRTMP`.
2. **Modo Autónomo completo:** Si `Pelican4Ever.SVRTMP` no está instalado o está desactivado (`Enabled = false`), **PelicanChat** provee todas sus capacidades por sí mismo: historial de `200` mensajes por canal, scroll, pestañas, comandos `/l`/`/g`/`/w`/`/d` y puente Discord opcional en el Host.
3. **Detección e Integración en caliente:** En `GameLoop.GameLaunched`, **PelicanChat** consulta `Helper.ModRegistry.GetApi<IPelican4EverChatApi>("Pelican4Ever.SVRTMP")`. Cuando está disponible y activo:
   - Evita colisiones en `ChatCommands` cooperando con `ChatChannelModule`.
   - Sincroniza marcas temporales con el reloj 1:1 del servidor.
   - Clasifica mensajes de los módulos de `Pelican 4Ever` en la pestaña `Eventos`.
   - Enruta el puente de Discord a través del canal oficial `/api/v1/host/chat` y `HostEventLog` (`chat.message`).
4. **Lógica pura aislada:** Siguiendo la convención de `Pelican4Ever`, toda lógica sin dependencias gráficas reside en `PelicanChat.Core` (`net6.0`) probada mediante `xUnit` en `PelicanChat.Tests` (`net8.0`).

## Consecuencias

* **Positivas:** Desacoplamiento limpio entre UI de chat y reglas de mundo persistente; reutilizable en partidas vanilla y totalmente integrado en servidores `Pelican 4Ever`; pruebas unitarias en milisegundos sin arrancar el juego.
* **Negativas:** El coordinador del modpack de `Pelican 4Ever` debe decidir si añade `"Pelican4Ever.Chat"` a `RequiredClientMods` en `config.json` del servidor (recomendado si se usa el canal `/d` unificado) o lo mantiene opcional en clientes.
