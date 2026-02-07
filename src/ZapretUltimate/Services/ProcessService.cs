using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZapretUltimate.Models;

namespace ZapretUltimate.Services;

public class ProcessService
{
    private readonly List<Process> _runningProcesses = new();
    private readonly string _basePath;
    private readonly string _binPath;
    private readonly ConfigService _configService;

    public event EventHandler<bool>? StatusChanged;
    public bool IsRunning => _runningProcesses.Count > 0 && _runningProcesses.Any(p => !p.HasExited);

    public ProcessService(ConfigService configService)
    {
        _configService = configService;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        _binPath = Path.Combine(_basePath, "bin");
    }

    public async Task StartAsync(IEnumerable<ZapretConfig> configs, AppSettings settings)
    {
        await StopAsync();

        var sortedConfigs = configs
            .OrderBy(c => c.Category.GetPriority())
            .ToList();

        foreach (var config in sortedConfigs)
        {
            var configPath = settings.IpsetGlobalEnabled || settings.IpsetGamingEnabled
                ? _configService.CreateDynamicConfig(config, settings.IpsetGlobalEnabled, settings.IpsetGamingEnabled)
                : config.FilePath;

            await StartWinwsProcess(configPath, config.FileName, settings.ShowLogs);
        }

        // Даем процессам время запуститься и проверяем статус
        await Task.Delay(500);
        var running = _runningProcesses.Count > 0 && _runningProcesses.Any(p => !p.HasExited);
        StatusChanged?.Invoke(this, running);
    }

    private async Task StartWinwsProcess(string configPath, string configName, bool showWindow)
    {
        var winwsPath = Path.Combine(_binPath, "winws.exe");

        if (!File.Exists(winwsPath))
            throw new FileNotFoundException("winws.exe not found", winwsPath);

        var configContent = File.ReadAllText(configPath);
        var lines = configContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Объединяем все строки конфига в одну командную строку
        // Конфиг - это один набор аргументов для winws, где правила разделены через --new
        var allArgs = new List<string>();
        foreach (var line in lines)
        {
            var args = line.Trim();
            if (string.IsNullOrEmpty(args) || args.StartsWith('#'))
                continue;
            allArgs.Add(args);
        }

        if (allArgs.Count == 0)
            return;

        var combinedArgs = string.Join(" ", allArgs);

        var startInfo = new ProcessStartInfo
        {
            FileName = winwsPath,
            Arguments = combinedArgs,
            WorkingDirectory = _basePath,  // Корень приложения, чтобы пути lists\ и bin\ работали
            UseShellExecute = false,
            CreateNoWindow = !showWindow,
            WindowStyle = showWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
        };

        try
        {
            var process = Process.Start(startInfo);
            if (process != null)
            {
                _runningProcesses.Add(process);
                Debug.WriteLine($"Started winws for {configName}: {combinedArgs.Substring(0, Math.Min(100, combinedArgs.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start process: {ex.Message}");
        }

        await Task.Delay(100);
    }

    public async Task StopAsync()
    {
        foreach (var process in _runningProcesses)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
            }
            catch { }
        }
        _runningProcesses.Clear();

        await KillAllWinwsProcesses();

        _configService.CleanupTempConfigs();
        StatusChanged?.Invoke(this, false);
    }

    public static async Task KillAllWinwsProcesses()
    {
        try
        {
            var processes = Process.GetProcessesByName("winws");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
                catch { }
            }
        }
        catch { }
    }

    public List<string> CheckConflictingProcesses()
    {
        var conflicts = new List<string>();
        var conflictingNames = new[]
        {
            ("goodbyedpi", "GoodbyeDPI"),
            ("openvpn", "OpenVPN"),
            ("wireguard", "WireGuard"),
            ("protonvpn", "Proton VPN"),
            ("tun2socks", "tun2socks")
        };

        foreach (var (processName, displayName) in conflictingNames)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    conflicts.Add(displayName);
                }
            }
            catch { }
        }

        return conflicts;
    }
}
