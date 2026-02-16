using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using ZapretUltimate.Resources;
using ZapretUltimate.ViewModels;
using ZapretUltimate.Views;

namespace ZapretUltimate;

public partial class App : Application
{
    private const string MutexName = "ZapretUltimate_SingleInstance_Mutex";
    private const string EventName = "ZapretUltimate_ShowWindow_Event";

    private static Mutex? _mutex;
    private EventWaitHandle? _showWindowEvent;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;
    private bool _isAutoStart;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        ListenForShowWindowSignal();

        _isAutoStart = e.Args.Contains("--autostart");

        try
        {
            IconGenerator.GenerateIcons();
        }
        catch { }

        _viewModel = new MainViewModel();

        _trayIcon = new TaskbarIcon
        {
            Icon = LoadIcon("tray.ico"),
            ToolTipText = "Zapret Ultimate",
            DoubleClickCommand = new SimpleCommand(ShowMainWindow)
        };

        _trayIcon.ContextMenu = CreateTrayContextMenu();

        _viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.IsRunning))
            {
                UpdateTrayIcon();
            }
        };

        _mainWindow = new MainWindow
        {
            DataContext = _viewModel
        };

        _mainWindow.Closing += (s, args) =>
        {
            args.Cancel = true;
            _mainWindow.Hide();
        };

        if (_isAutoStart)
        {
            await _viewModel.HandleAutoStart();
        }
        else
        {
            _mainWindow.Show();
        }
    }

    private System.Drawing.Icon LoadIcon(string name)
    {
        var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", name);
        if (System.IO.File.Exists(path))
        {
            return new System.Drawing.Icon(path);
        }

        return CreateDefaultIcon();
    }

    private static System.Drawing.Icon CreateDefaultIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(System.Drawing.Color.DodgerBlue);
        g.FillEllipse(System.Drawing.Brushes.White, 4, 4, 24, 24);
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null || _viewModel == null) return;

        var iconName = _viewModel.IsRunning ? "tray_active.ico" : "tray.ico";
        _trayIcon.Icon = LoadIcon(iconName);
        _trayIcon.ToolTipText = _viewModel.IsRunning
            ? "Zapret Ultimate - Работает"
            : "Zapret Ultimate - Остановлено";
    }

    private System.Windows.Controls.ContextMenu CreateTrayContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Открыть" };
        showItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var startItem = new System.Windows.Controls.MenuItem { Header = "Запустить" };
        startItem.Click += async (s, e) => { if (_viewModel != null) await _viewModel.StartCommand.ExecuteAsync(null); };
        menu.Items.Add(startItem);

        var stopItem = new System.Windows.Controls.MenuItem { Header = "Остановить" };
        stopItem.Click += async (s, e) => { if (_viewModel != null) await _viewModel.StopCommand.ExecuteAsync(null); };
        menu.Items.Add(stopItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
        exitItem.Click += (s, e) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var evt = EventWaitHandle.OpenExisting(EventName);
            evt.Set();
        }
        catch { }
    }

    private void ListenForShowWindowSignal()
    {
        var thread = new Thread(() =>
        {
            while (_showWindowEvent != null && _showWindowEvent.WaitOne())
            {
                Dispatcher.Invoke(ShowMainWindow);
            }
        })
        {
            IsBackground = true
        };
        thread.Start();
    }

    private async void ExitApplication()
    {
        if (_viewModel != null)
        {
            await _viewModel.CleanupAsync();
        }

        _trayIcon?.Dispose();
        _showWindowEvent?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        Shutdown();
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.CleanupAsync();
        }

        _trayIcon?.Dispose();
        _showWindowEvent?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}

public class SimpleCommand : ICommand
{
    private readonly Action _execute;

    public SimpleCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
