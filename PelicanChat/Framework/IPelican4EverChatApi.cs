namespace PelicanChat.Framework
{
    /// <summary>
    /// Interfaz espejo de SMAPI Mod API consumida desde Pelican4Ever.SVRTMP.
    /// Permite acoplar PelicanChat sin referenciar Pelican4Ever.dll.
    /// </summary>
    public interface IPelican4EverChatApi
    {
        bool IsModEnabled();
        string GetServerTimeFormatted();
        void PublishChatEvent(string channel, string senderName, string message);
    }
}
