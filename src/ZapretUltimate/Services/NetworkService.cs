using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ZapretUltimate.Services;

public class NetworkService
{
    public async Task ResetNetworkAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Сброс настроек прокси...");
        await ResetProxySettingsAsync();

        progress?.Report("Сброс WinHTTP прокси...");
        await RunCommandAsync("netsh", "winhttp reset proxy");

        progress?.Report("Сброс переменных окружения...");
        RemoveProxyEnvironmentVariables();

        progress?.Report("Сброс Winsock...");
        await RunCommandAsync("netsh", "winsock reset");

        progress?.Report("Очистка DNS кеша...");
        await RunCommandAsync("ipconfig", "/flushdns");

        progress?.Report("Сетевые настройки сброшены!");
    }

    private static async Task ResetProxySettingsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                const string internetSettingsPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
                using var key = Registry.CurrentUser.OpenSubKey(internetSettingsPath, true);
                if (key != null)
                {
                    key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    key.DeleteValue("ProxyServer", false);
                    key.DeleteValue("ProxyOverride", false);
                }
            }
            catch { }
        });
    }

    private static void RemoveProxyEnvironmentVariables()
    {
        var proxyVars = new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "NO_PROXY" };

        foreach (var varName in proxyVars)
        {
            try
            {
                Environment.SetEnvironmentVariable(varName, null, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable(varName.ToLower(), null, EnvironmentVariableTarget.User);
            }
            catch { }
        }
    }

    private static async Task RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch { }
    }
}
