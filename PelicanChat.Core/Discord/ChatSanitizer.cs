using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PelicanChat.Core.Discord
{
    /// <summary>
    /// Sanitiza texto externo (Discord / API HTTP) antes de pasarlo a StardewValley.Menus.ChatMessage.parseMessageForEmoji
    /// y Fonts\SmallFont para evitar inyecciones de color/emoji, desbordamientos o caracteres no renderizables.
    /// </summary>
    public static class ChatSanitizer
    {
        public const int DefaultMaxLength = 280;
        public const int MaxAuthorLength = 32;

        private static readonly Regex MentionUserRegex = new(@"<@!?(\d+)>", RegexOptions.Compiled);
        private static readonly Regex MentionRoleRegex = new(@"<@&(\d+)>", RegexOptions.Compiled);
        private static readonly Regex MentionChannelRegex = new(@"<#(\d+)>", RegexOptions.Compiled);
        private static readonly Regex CustomEmojiRegex = new(@"<a?:([A-Za-z0-9_]+):\d+>", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRunRegex = new(@"\s+", RegexOptions.Compiled);

        public static string SanitizeAuthor(string? rawAuthor)
        {
            if (string.IsNullOrWhiteSpace(rawAuthor))
                return "Discord";

            string cleaned = StripUnsupportedChars(rawAuthor);
            cleaned = cleaned.Replace('[', '(').Replace(']', ')');
            cleaned = WhitespaceRunRegex.Replace(cleaned, " ").Trim();

            if (string.IsNullOrEmpty(cleaned))
                return "Discord";

            return cleaned.Length > MaxAuthorLength
                ? cleaned.Substring(0, MaxAuthorLength)
                : cleaned;
        }

        public static string SanitizeForChatBox(string? rawMessage, int maxLength = DefaultMaxLength)
        {
            if (maxLength <= 0)
                maxLength = DefaultMaxLength;

            if (string.IsNullOrWhiteSpace(rawMessage))
                return string.Empty;

            string text = rawMessage;

            // Convertir menciones y emojis personalizados de Discord en etiquetas legibles
            text = CustomEmojiRegex.Replace(text, ":$1:");
            text = MentionUserRegex.Replace(text, "@usuario");
            text = MentionRoleRegex.Replace(text, "@rol");
            text = MentionChannelRegex.Replace(text, "#canal");

            // Neutralizar corchetes para evitar tags [red], [aqua], [0..totalEmojis] o truncados en parseMessageForEmoji
            text = text.Replace('[', '(').Replace(']', ')');

            // Eliminar pares subrogados (emojis Unicode fuera de BMP) y caracteres de control
            text = StripUnsupportedChars(text);

            // Normalizar saltos de línea y espacios múltiples
            text = WhitespaceRunRegex.Replace(text, " ").Trim();

            if (text.Length > maxLength)
            {
                int cut = Math.Max(1, maxLength - 3);
                text = text.Substring(0, cut).TrimEnd() + "...";
            }

            return text;
        }

        private static string StripUnsupportedChars(string input)
        {
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Saltar pares subrogados (emojis Unicode modernos que SmallFont no soporta)
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                        i++;
                    continue;
                }
                if (char.IsLowSurrogate(c))
                    continue;

                if (c is '\r' or '\n' or '\t')
                {
                    sb.Append(' ');
                    continue;
                }

                if (char.IsControl(c))
                    continue;

                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.Format)
                    continue;

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
