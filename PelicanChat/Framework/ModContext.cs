using HarmonyLib;
using PelicanChat.Core.Config;
using PelicanChat.Core.Viewport;
using PelicanChat.Integration;
using StardewModdingAPI;

namespace PelicanChat.Framework
{
    /// <summary>
    /// Contexto compartido por los módulos y parches Harmony de PelicanChat.
    /// </summary>
    public sealed class ModContext
    {
        public static ModContext Current { get; private set; } = null!;

        public IModHelper Helper { get; }
        public IMonitor Monitor { get; }
        public IManifest Manifest { get; }
        public ChatConfig Config { get; }
        public Harmony Harmony { get; }
        public HarmonyPatchToggle PatchToggle { get; } = new();
        public ChatViewState ViewState { get; }
        public Pelican4EverBridge PelicanBridge { get; }

        public ModContext(IModHelper helper, IMonitor monitor, IManifest manifest, ChatConfig config, Harmony harmony)
        {
            Helper = helper;
            Monitor = monitor;
            Manifest = manifest;
            Config = config;
            Harmony = harmony;
            ViewState = new ChatViewState(config);
            PelicanBridge = new Pelican4EverBridge(helper, monitor, config);
            Current = this;
        }
    }
}
