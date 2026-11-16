using System;
using PelicanChat.Framework;
using PelicanChat.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace PelicanChat.Modules
{
    /// <summary>
    /// Gestiona el ciclo de vida de la vista mejorada de ChatBox, los parches Harmony y el comando pchat_status.
    /// </summary>
    public sealed class ChatViewModule : IModule
    {
        private ModContext? context;

        public string Name => "ChatView";

        public ModuleSide Side => ModuleSide.Both;

        public void Register(ModContext ctx)
        {
            context = ctx;

            if (ctx.Config.Enabled)
            {
                ChatBoxPatches.Apply(ctx.Harmony);
                ctx.PatchToggle.CaptureOwnedPatches(ctx.Harmony);
            }

            ctx.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            ctx.Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            ctx.Helper.ConsoleCommands.Add(
                "pchat_status",
                "Muestra el estado actual de PelicanChat (modo autónomo/integrado, pestaña, buffers y puente Discord).",
                OnStatusCommand);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            context?.PelicanBridge.Initialize();
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            ChatBoxPatches.ClearCache();
        }

        private void OnStatusCommand(string command, string[] args)
        {
            if (context == null)
                return;

            string mode = context.PelicanBridge.IsPelican4EverActive
                ? "Integrado con Pelican 4Ever"
                : "Autónomo (vanilla/co-op)";

            var tab = context.ViewState.CurrentTab;
            context.Monitor.Log(
                $"[PelicanChat] Estado={context.Config.Enabled} | Modo={mode} | Pestaña={context.ViewState.ActiveTab} " +
                $"| Msgs(pestaña)={tab.Buffer.Count}/{tab.Buffer.Capacity} | ScrollOffset={tab.ScrollOffsetPx}px " +
                $"| DiscordBridge={context.Config.DiscordBridgeEnabled}",
                LogLevel.Info);
        }
    }
}
