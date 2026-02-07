using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ZapretUltimate.Models;

public class AppSettings
{
    public bool IpsetGlobalEnabled { get; set; }
    public bool IpsetGamingEnabled { get; set; }
    public bool ShowLogs { get; set; }
    public bool AutoStart { get; set; }
    public bool StartMinimized { get; set; } = true;
    public bool LaunchOnStartup { get; set; }
    public List<string> SelectedConfigs { get; set; } = new();
    public List<string> AutorunConfigs { get; set; } = new();

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "settings.json"
    );

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Ignore errors, return default settings
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
