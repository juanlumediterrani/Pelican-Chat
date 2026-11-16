using System;

namespace PelicanChat.Framework
{
    [Flags]
    public enum ModuleSide
    {
        Host = 1,
        Client = 2,
        Both = Host | Client
    }

    public interface IModule
    {
        string Name { get; }
        ModuleSide Side { get; }
        void Register(ModContext context);
    }
}
