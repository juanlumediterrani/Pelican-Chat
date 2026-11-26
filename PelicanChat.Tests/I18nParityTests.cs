using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace PelicanChat.Tests;

public class I18nParityTests
{
    private static readonly string[] TranslationFiles = { "default.json", "es.json", "zh-CN.json" };

    [Fact]
    public void AllTranslations_HaveExactKeyParityAndNonEmptyValues()
    {
        string root = FindRepoRoot();
        string i18nDir = Path.Combine(root, "PelicanChat", "i18n");
        Assert.True(Directory.Exists(i18nDir), $"No se encontró {i18nDir}");

        var maps = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (string fileName in TranslationFiles)
        {
            string fullPath = Path.Combine(i18nDir, fileName);
            Assert.True(File.Exists(fullPath), $"Falta archivo de traducción {fullPath}");

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fullPath));
            Assert.NotNull(parsed);
            Assert.NotEmpty(parsed!);

            foreach ((string key, string value) in parsed!)
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"Valor vacío para '{key}' en {fileName}.");
            }

            maps[fileName] = parsed!;
        }

        string[] referenceKeys = maps["default.json"].Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        foreach (string fileName in TranslationFiles.Skip(1))
        {
            string[] currentKeys = maps[fileName].Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
            Assert.Equal(referenceKeys, currentKeys);
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PelicanChat.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return Path.GetFullPath(".");
    }
}
