using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZapretUltimate.Models;
using ZapretUltimate.Services;

namespace ZapretUltimate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly ProcessService _processService;
    private readonly AutoStartService _autoStartService;
    private readonly NetworkService _networkService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Остановлено";

    [ObservableProperty]
    private bool _ipsetGlobalEnabled;

    [ObservableProperty]
    private bool _ipsetGamingEnabled;

    [ObservableProperty]
    private bool _showLogs;

    [ObservableProperty]
    private bool _autoStartEnabled;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingText = string.Empty;

    public ObservableCollection<ConfigCategoryViewModel> Categories { get; } = new();

    public MainViewModel()
    {
        _configService = new ConfigService();
        _processService = new ProcessService(_configService);
        _autoStartService = new AutoStartService();
        _networkService = new NetworkService();
        _settings = AppSettings.Load();

        _processService.StatusChanged += OnProcessStatusChanged;

        LoadSettings();
        LoadConfigs();
    }

    private void LoadSettings()
    {
        IpsetGlobalEnabled = _settings.IpsetGlobalEnabled;
        IpsetGamingEnabled = _settings.IpsetGamingEnabled;
        ShowLogs = _settings.ShowLogs;
        AutoStartEnabled = _autoStartService.IsAutoStartEnabled();
    }

    private void LoadConfigs()
    {
        var configs = _configService.LoadAllConfigs();

        foreach (ConfigCategory category in Enum.GetValues<ConfigCategory>())
        {
            var categoryConfigs = configs
                .Where(c => c.Category == category)
                .ToList();

            var categoryVm = new ConfigCategoryViewModel(category, categoryConfigs);

            categoryVm.SelectedConfig = categoryConfigs
                .FirstOrDefault(c => _settings.SelectedConfigs.Contains(c.FilePath));

            Categories.Add(categoryVm);
        }
    }

    partial void OnIpsetGlobalEnabledChanged(bool value)
    {
        _settings.IpsetGlobalEnabled = value;
        _settings.Save();
    }

    partial void OnIpsetGamingEnabledChanged(bool value)
    {
        _settings.IpsetGamingEnabled = value;
        _settings.Save();
    }

    partial void OnShowLogsChanged(bool value)
    {
        _settings.ShowLogs = value;
        _settings.Save();
    }

    partial void OnAutoStartEnabledChanged(bool value)
    {
        if (value)
        {
            _autoStartService.EnableAutoStart();
        }
        else
        {
            _autoStartService.DisableAutoStart();
        }
    }

    private void OnProcessStatusChanged(object? sender, bool isRunning)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsRunning = isRunning;
            StatusText = isRunning ? "Работает" : "Остановлено";
        });
    }

    [RelayCommand]
    private async Task Start()
    {
        var conflicts = _processService.CheckConflictingProcesses();
        if (conflicts.Count > 0)
        {
            var message = $"Обнаружены конфликтующие программы:\n{string.Join("\n", conflicts)}\n\nОни могут мешать работе Zapret. Продолжить?";
            var result = MessageBox.Show(message, "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }

        var selectedConfigs = Categories
            .Select(c => c.SelectedConfig)
            .Where(c => c != null)
            .Cast<ZapretConfig>()
            .ToList();

        if (selectedConfigs.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы один конфиг", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.SelectedConfigs = selectedConfigs.Select(c => c.FilePath).ToList();
        _settings.Save();

        IsLoading = true;
        LoadingText = "Запуск...";

        try
        {
            await _processService.StartAsync(selectedConfigs, _settings);
            StatusText = $"Работает ({selectedConfigs.Count} конфиг.)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка запуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Stop()
    {
        IsLoading = true;
        LoadingText = "Остановка...";

        try
        {
            await _processService.StopAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Restart()
    {
        await Stop();
        await Task.Delay(500);
        await Start();
    }

    [RelayCommand]
    private async Task ResetNetwork()
    {
        var result = MessageBox.Show(
            "Сброс сетевых настроек очистит прокси и DNS кеш.\n\nПосле сброса может потребоваться перезагрузка.\n\nПродолжить?",
            "Сброс сети",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        IsLoading = true;

        try
        {
            var progress = new Progress<string>(msg => LoadingText = msg);
            await _networkService.ResetNetworkAsync(progress);

            var rebootResult = MessageBox.Show(
                "Сетевые настройки сброшены.\n\nХотите перезагрузить компьютер сейчас?",
                "Готово",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (rebootResult == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start("shutdown", "/r /t 5");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task HandleAutoStart()
    {
        var selectedConfigs = Categories
            .Select(c => c.SelectedConfig)
            .Where(c => c != null)
            .Cast<ZapretConfig>()
            .ToList();

        if (selectedConfigs.Count > 0)
        {
            await _processService.StartAsync(selectedConfigs, _settings);
        }
    }

    public async Task CleanupAsync()
    {
        await _processService.StopAsync();
    }
}

public partial class ConfigCategoryViewModel : ObservableObject
{
    public ConfigCategory Category { get; }
    public string Name => Category.GetDisplayName();
    public ObservableCollection<ZapretConfig> Configs { get; }

    [ObservableProperty]
    private ZapretConfig? _selectedConfig;

    public ConfigCategoryViewModel(ConfigCategory category, IEnumerable<ZapretConfig> configs)
    {
        Category = category;
        Configs = new ObservableCollection<ZapretConfig>(configs);
    }
}
