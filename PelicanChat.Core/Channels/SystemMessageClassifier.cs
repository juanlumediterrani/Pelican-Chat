using System;

namespace PelicanChat.Core.Channels
{
    public readonly record struct ClassificationResult(ChatChannelType Channel, bool IsCritical);

    /// <summary>
    /// Clasifica mensajes entrantes de ChatBox (vanilla y módulos de Pelican 4Ever) en su pestaña correspondiente.
    /// </summary>
    public static class SystemMessageClassifier
    {
        private static readonly string[] SystemPrefixes =
        {
            "[Festival]",
            "[Ceremonia]",
            "[Reinicio Controlado]",
            "[Rollover]",
            "[Madrugada]",
            "[Mercado]",
            "[Correo]",
            "[Pelican 4Ever]",
            "[Ceremony]",
            "[Market]",
            "[Mail]",
            "[Controlled Restart]",
            "[节日]",
            "[仪式]"
        };

        private static readonly string[] CriticalPrefixes =
        {
            "[Reinicio Controlado]",
            "[Controlled Restart]",
            "[Rollover]"
        };

        public static ClassificationResult Classify(int chatKind, string? text, ChatColor color)
        {
            string trimmed = (text ?? string.Empty).TrimStart();

            if (trimmed.StartsWith("[Local]", StringComparison.OrdinalIgnoreCase) ||
                (color.R == 130 && color.G == 230 && color.B == 130))
            {
                return new ClassificationResult(ChatChannelType.Local, false);
            }

            if (trimmed.StartsWith("[Discord]", StringComparison.OrdinalIgnoreCase) ||
                (color.R == 114 && color.G == 137 && color.B == 218))
            {
                return new ClassificationResult(ChatChannelType.Discord, false);
            }

            if (chatKind == 3)
            {
                return new ClassificationResult(ChatChannelType.Whisper, false);
            }

            if (chatKind is 1 or 2)
            {
                bool critical = IsCriticalPrefix(trimmed);
                return new ClassificationResult(ChatChannelType.System, critical);
            }

            foreach (string prefix in SystemPrefixes)
            {
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return new ClassificationResult(ChatChannelType.System, IsCriticalPrefix(trimmed));
                }
            }

            // Avisos dorados de Pelican 4Ever (AdminApiModule / FestivalMinigameModule: 255, 215, 0)
            if (color.R == 255 && color.G == 215 && color.B == 0)
            {
                return new ClassificationResult(ChatChannelType.System, IsCriticalPrefix(trimmed));
            }

            return new ClassificationResult(ChatChannelType.Global, false);
        }

        private static bool IsCriticalPrefix(string trimmed)
        {
            foreach (string critical in CriticalPrefixes)
            {
                if (trimmed.StartsWith(critical, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
