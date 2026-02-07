using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

namespace ZapretUltimate.Services;

public class AutoStartService
{
    private const string TaskName = "ZapretUltimate_Autorun";
    private const string RegistryRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ZapretUltimate";

    public bool IsAutoStartEnabled()
    {
        // Check Task Scheduler first
        if (IsTaskSchedulerEnabled())
            return true;

        // Fallback to registry check
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTaskSchedulerEnabled()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Query /TN \"{TaskName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool EnableAutoStart()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
            return false;

        // Use Task Scheduler for admin apps (preferred method)
        if (IsRunningAsAdmin())
        {
            return EnableViaTaskScheduler(exePath);
        }

        // Fallback to registry (may not work with admin apps)
        return EnableViaRegistry(exePath);
    }

    public bool DisableAutoStart()
    {
        var success = true;

        // Remove from Task Scheduler
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch
        {
            success = false;
        }

        // Remove from registry
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
            key?.DeleteValue(AppName, false);
        }
        catch
        {
            success = false;
        }

        return success;
    }

    private static bool EnableViaTaskScheduler(string exePath)
    {
        try
        {
            // First, delete existing task if any
            var deleteInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var deleteProcess = Process.Start(deleteInfo))
            {
                deleteProcess?.WaitForExit();
            }

            // Create new task with highest privileges, triggered on logon
            var createInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\" --autostart\" /SC ONLOGON /RL HIGHEST /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(createInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool EnableViaRegistry(string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true);
            key?.SetValue(AppName, $"\"{exePath}\" --autostart");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
