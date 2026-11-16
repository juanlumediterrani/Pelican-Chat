using System;
using System.Collections.Generic;
using HarmonyLib;
using PelicanChat.Core.Config;
using PelicanChat.Framework;
using PelicanChat.Modules;
using StardewModdingAPI;

namespace PelicanChat
{
    /// <summary>
    /// Punto de entrada SMAPI de PelicanChat. Funciona autónomamente o acoplado a Pelican4Ever.SVRTMP.
    /// </summary>
    public sealed class ModEntry : Mod
    {
        private ModContext? context;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            ChatConfig config = helper.ReadConfig<ChatConfig>();
            IReadOnlyList<string> errors = config.Validate();
            if (errors.Count > 0)
            {
                foreach (string error in errors)
                    Monitor.Log($"config.json: {error}", LogLevel.Error);
                Monitor.Log("Configuración inválida: PelicanChat queda desactivado hasta corregir config.json.", LogLevel.Error);
                return;
            }

            context = new ModContext(helper, Monitor, ModManifest, config, new Harmony(ModManifest.UniqueID));

            RegisterModules(
                new ChatViewModule(),
                new ChatTransportModule(),
                new DiscordBridgeModule()
            );

            Monitor.Log($"PelicanChat cargado (pestaña inicial={config.ParsedDefaultTab}, historial={config.MaxHistoryPerChannel}/canal).", LogLevel.Info);
        }

        private void RegisterModules(params IModule[] modules)
        {
            if (context == null)
                return;

            foreach (IModule module in modules)
            {
                try
                {
                    module.Register(context);
                    Monitor.Log($"Módulo '{module.Name}' ({module.Side}) registrado.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Módulo '{module.Name}' falló al registrarse:\n{ex}", LogLevel.Error);
                }
            }
        }
    }
}
