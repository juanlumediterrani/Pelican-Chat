using System;
using StardewModdingAPI;

namespace PelicanChat.Framework
{
    internal static class I18n
    {
        private static ITranslationHelper? translations;

        public static void Init(ITranslationHelper helper)
        {
            translations = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public static string Get(string key, object? tokens = null)
        {
            if (translations == null)
                return key;

            return tokens == null
                ? translations.Get(key)
                : translations.Get(key, tokens);
        }
    }
}
