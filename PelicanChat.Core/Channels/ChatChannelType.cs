namespace PelicanChat.Core.Channels
{
    /// <summary>
    /// Canales disponibles como pestañas en el ChatBox.
    /// </summary>
    public enum ChatChannelType
    {
        All = 0,
        Local = 1,
        Global = 2,
        Discord = 3,
        Whisper = 4,
        System = 5
    }

    /// <summary>
    /// Origen de una entrada de chat para evitar bucles de eco hacia Discord/API.
    /// </summary>
    public enum ExternalOrigin
    {
        None = 0,
        Discord = 1,
        HostAgent = 2
    }
}
