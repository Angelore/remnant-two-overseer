using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System;
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;

internal partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly SaveDataService _saveDataService;
    private readonly BackupService _backupService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisplayContent))]
    private ViewModelBase _contentViewModel;

    [ObservableProperty]
    private bool _isCharacterViewSelected;

    [ObservableProperty]
    private bool _isWorldViewSelected;

    [ObservableProperty]
    private bool _isMissingItemsViewSelected;

    [ObservableProperty]
    private bool _isBackupsViewSelected;

    [ObservableProperty]
    private bool _isSettingsViewSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDisplayContent))]
    private bool _isInErrorState = false;

    [ObservableProperty]
    private Character? _selectedCharacter;

    public bool CanDisplayContent => !IsInErrorState || ContentViewModel is SettingsViewModel;

    public WindowNotificationManager? NotificationManager { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable. (Is set in SwitchToWorldView)
    public MainWindowViewModel(SettingsService settingsService, SaveDataService saveDataService, BackupService backupService)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _settingsService = settingsService;
        _saveDataService = saveDataService;
        _backupService = backupService;
        IsActive = true; // Turn on the messenger https://github.com/CommunityToolkit/MVVM-Samples/issues/37
    }

    [RelayCommand]
    public void SwitchToWorldView()
    {
        if (ContentViewModel is WorldViewModel) return;
        ContentViewModel = App.Resolve<WorldViewModel>();
        IsWorldViewSelected = true;
    }

    [RelayCommand]
    public void SwitchToCharacterView()
    {
        if (ContentViewModel is CharacterSelectViewModel) return;
        ContentViewModel = App.Resolve<CharacterSelectViewModel>();
        IsCharacterViewSelected = true;
    }

    [RelayCommand]
    public void SwitchToMissingItemsView()
    {
        if (ContentViewModel is MissingItemsViewModel) return;
        ContentViewModel = App.Resolve<MissingItemsViewModel>();
        IsMissingItemsViewSelected = true;
    }

    [RelayCommand]
    public void SwitchToBackupsView()
    {
        if (ContentViewModel is BackupViewModel) return;
        ContentViewModel = App.Resolve<BackupViewModel>();
        IsBackupsViewSelected = true;
    }

    [RelayCommand]
    public void SwitchToSettingsView()
    {
        if (ContentViewModel is SettingsViewModel) return;
        ContentViewModel = App.Resolve<SettingsViewModel>();
        IsSettingsViewSelected = true;
    }

    // This was moved out of the ctor to support early messages from the settings service
    // Otherwise everything inits before notification manager gets attached
    public void OnLoaded()
    {
        _settingsService.Initialize();
        _backupService.Initialize();
        SaveFileUpdatedHandler(true);
        SwitchToWorldView();
        _saveDataService.StartWatching();

#if DEBUG
        // Avoid being rate limited by gh
        return;
#endif
        // Version check
        Task.Run(async () =>
        {
            var nv = await VersionChecker.TryGetNewVersion();
            if (nv is not null)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    NotificationManager?.Show(new Notification("Information", string.Format(NotificationStrings.NewerVersionFound, nv), NotificationType.Information));
                });
        });
    }

    #region Messages
    protected override void OnActivated()
    {
        // This should be replaced by a navigator in the future
        Messenger.Register<MainWindowViewModel, CharacterSelectChangedMessage>(this, (r, m) => {
            CharacterUpdatedHandler(m.Value);
            SwitchToWorldViewCommand.Execute(null);
        });

        Messenger.Register<MainWindowViewModel, SaveFileChangedMessage>(this, (r, m) => {
            SaveFileUpdatedHandler(m.CharacterCountChanged);
        });

        Messenger.Register<MainWindowViewModel, DatasetIsNullMessage>(this, (r, m) => {
            IsInErrorState = true;
        });

        Messenger.Register<MainWindowViewModel, DatasetParsedMessage>(this, (r, m) => {
            IsInErrorState = false;
        });

        Messenger.Register<MainWindowViewModel, NotificationErrorMessage>(this, (r, m) => {
            NotificationManager?.Show(new Notification("Error", m.Value, NotificationType.Error, TimeSpan.Zero));
        });

        Messenger.Register<MainWindowViewModel, NotificationWarningMessage>(this, (r, m) => {
            NotificationManager?.Show(new Notification("Warning", m.Value, NotificationType.Warning));
        });

        Messenger.Register<MainWindowViewModel, NotificationInfoMessage>(this, (r, m) => {
            NotificationManager?.Show(new Notification("Information", m.Value, NotificationType.Information));
        });
    }

    private void CharacterUpdatedHandler(int index)
    {
        Task.Run(async () =>
        {
            var ds = await _saveDataService.GetSaveData();
            if (ds == null)
            {
                SelectedCharacter = null;
                return;
            }
            SelectedCharacter = DatasetMapper.MapCharacter(ds.Characters[index]);
        });
    }

    private void SaveFileUpdatedHandler(bool resetActiveCharacter)
    {
        Task.Run(async () =>
        {
            var ds = await _saveDataService.GetSaveData();
            if (ds == null)
            {
                SelectedCharacter = null;
                return;
            }
            var index = resetActiveCharacter ? ds.ActiveCharacterIndex : SelectedCharacter?.Index ?? 0;
            SelectedCharacter = DatasetMapper.MapCharacter(ds.Characters[index]);
        });
    }

    public void Dispose()
    {
        _saveDataService.PauseWatching();
    }
    #endregion Messages
}
