using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZapretUltimate.Models;

namespace ZapretUltimate.Services;

public class ConfigService
{
    private readonly string _configsPath;

    public ConfigService()
    {
        _configsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs");
    }

    public List<ZapretConfig> LoadAllConfigs()
    {
        var configs = new List<ZapretConfig>();

        foreach (ConfigCategory category in Enum.GetValues<ConfigCategory>())
        {
            configs.AddRange(LoadConfigsForCategory(category));
        }

        return configs;
    }

    public List<ZapretConfig> LoadConfigsForCategory(ConfigCategory category)
    {
        var configs = new List<ZapretConfig>();
        var folderPath = Path.Combine(_configsPath, category.GetFolderName());

        if (!Directory.Exists(folderPath))
            return configs;

        var files = Directory.GetFiles(folderPath, "*.conf");

        foreach (var file in files.OrderBy(f => f))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            configs.Add(new ZapretConfig
            {
                Name = FormatConfigName(fileName),
                FileName = fileName,
                FilePath = file,
                Category = category
            });
        }

        return configs;
    }

    public string? ReadConfigContent(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);
        }
        catch
        {
            // Ignore read errors
        }
        return null;
    }

    public string CreateDynamicConfig(ZapretConfig config, bool useIpsetGlobal, bool useIpsetGaming)
    {
        var content = ReadConfigContent(config.FilePath);
        if (string.IsNullOrEmpty(content))
            return config.FilePath;

        var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_configs");
        Directory.CreateDirectory(tempDir);

        var lines = content.Split('\n').Select(line =>
        {
            var trimmedLine = line.Trim();

            if (config.Category == ConfigCategory.Gaming && useIpsetGaming)
            {
                if (trimmedLine.Contains("--hostlist="))
                {
                    trimmedLine = RemoveParameter(trimmedLine, "--hostlist=");
                }
            }
            else if (useIpsetGlobal && config.Category != ConfigCategory.Gaming)
            {
                if (trimmedLine.Contains("--hostlist="))
                {
                    trimmedLine = RemoveParameter(trimmedLine, "--hostlist=");
                }
            }

            return trimmedLine;
        });

        var tempPath = Path.Combine(tempDir, $"dynamic_{config.FileName}.conf");
        File.WriteAllText(tempPath, string.Join("\n", lines));

        return tempPath;
    }

    private static string RemoveParameter(string line, string paramPrefix)
    {
        var startIndex = line.IndexOf(paramPrefix);
        if (startIndex == -1) return line;

        var endIndex = startIndex + paramPrefix.Length;

        if (endIndex < line.Length && line[endIndex] == '"')
        {
            endIndex = line.IndexOf('"', endIndex + 1);
            if (endIndex != -1) endIndex++;
        }
        else
        {
            while (endIndex < line.Length && !char.IsWhiteSpace(line[endIndex]))
                endIndex++;
        }

        return (line[..startIndex] + line[endIndex..]).Trim();
    }

    private static string FormatConfigName(string fileName)
    {
        return fileName
            .Replace("_", " ")
            .Replace("-", " ");
    }

    public void CleanupTempConfigs()
    {
        var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_configs");
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
