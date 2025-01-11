using Avalonia.Controls;
using Avalonia.Interactivity;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using RemnantOverseer.ViewModels;
using System;

namespace RemnantOverseer.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        // https://docs.avaloniaui.net/docs/guides/implementation-guides/ide-support
        // Prevent the previewer's DataContext from being set when the application is run.
        if (Design.IsDesignMode)
        {
            // This can be before or after InitializeComponent.
            var settingsService = new Services.SettingsService();
            var saveDataService = new SaveDataService(settingsService);
            var backupService = new BackupService(settingsService, saveDataService);
            Design.SetDataContext(this, new MainWindowViewModel(settingsService, saveDataService, backupService));
        }
        InitializeComponent();
        Win32Properties.AddWndProcHookCallback(this, WndProcHook);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext as MainWindowViewModel is null) throw new Exception("DataContext is still empty");
        ((MainWindowViewModel)DataContext).NotificationManager = new Avalonia.Controls.Notifications.WindowNotificationManager(GetTopLevel(this));
        ((MainWindowViewModel)DataContext).NotificationManager!.Position = Avalonia.Controls.Notifications.NotificationPosition.BottomRight;
        ((MainWindowViewModel)DataContext).OnLoaded();
    }

    private nint WndProcHook(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NativeMethods.RO_WM_SHOWME)
        {
            ShowMe();
            handled = true;
        }
        return nint.Zero;
    }

    private void ShowMe()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        bool top = Topmost;
        Topmost = true;
        Topmost = top;
    }
}