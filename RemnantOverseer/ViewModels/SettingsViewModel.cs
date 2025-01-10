using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;
public partial class SettingsViewModel: ViewModelBase
{
    private const byte MIN_SPINNER = 1;
    private const byte MAX_SPINNER = 60;
    public static FilePickerFileType Saves { get; } = new("Remnant 2 save files")
    {
        Patterns = ["profile.sav"],
    };

    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string? _backupsPath;

    [ObservableProperty]
    private bool _isRollingBackupsEnabled;

    [ObservableProperty]
    private byte _amountOfRollingBackups;

    [ObservableProperty]
    private byte _minutesBetweenRollingBackups;

    private Subject<DateTime> _amountOfRollingBackupsSubject = new Subject<DateTime>();
    private Subject<DateTime> _minutesBetweenRollingBackupsSubject = new Subject<DateTime>();

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        FilePath = _settingsService.Get()?.SaveFilePath ?? null;
        BackupsPath = _settingsService.Get().BackupsPath ?? null;
        IsRollingBackupsEnabled = _settingsService.Get().RollingBackupsEnabled;
        AmountOfRollingBackups = _settingsService.Get().RollingBackupsAmount;
        MinutesBetweenRollingBackups = _settingsService.Get().MinutesBetweenRollingBackups;

        if(Design.IsDesignMode)
        {
            FilePath = @"C:\Remnant\Remnant 2: I'm Beginning to Remn";
            BackupsPath = FilePath + "\\Backups";
            IsRollingBackupsEnabled = true;
            AmountOfRollingBackups = 3;
            MinutesBetweenRollingBackups = 10;
        }

        _amountOfRollingBackupsSubject.Throttle(TimeSpan.FromSeconds(1)).Subscribe(events => OnAmountOfRollingBackupsChangedDebounced());
        _minutesBetweenRollingBackupsSubject.Throttle(TimeSpan.FromSeconds(1)).Subscribe(events => OnMinutesBetweenRollingBackupsChangedDebounced());
    }

    [RelayCommand]
    public async Task OpenFile()
    {
        var topLevel = FileDialogManager.GetTopLevelForContext(this);
        if (topLevel != null)
        {
            var storageFiles = await topLevel.StorageProvider.OpenFilePickerAsync(
                            new FilePickerOpenOptions()
                            {
                                FileTypeFilter = [Saves],
                                AllowMultiple = false,
                                Title = "Select the profile file"
                            });

            if (storageFiles.Count > 0)
            {
                var selectedFile = storageFiles.First();
                var settings = _settingsService.Get();
                var newPath = Path.GetDirectoryName(selectedFile.TryGetLocalPath());

                if (newPath == settings.SaveFilePath) return;
                
                settings.SaveFilePath = newPath;
                _settingsService.Update(settings);
                FilePath = newPath;
                WeakReferenceMessenger.Default.Send(new SaveFilePathChangedMessage(FilePath!));
                WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.SaveFileLocationChanged));
            }
        }
    }

    [RelayCommand(CanExecute = nameof(canexec))]
    public void RollingBackupsEnabled(bool value)
    {
        WeakReferenceMessenger.Default.Send(new RollingBackupsEnabledChangedMessage(IsRollingBackupsEnabled));
    }

    public bool canexec(bool v)
    {
        return true;
    }

    [RelayCommand]
    public void AmountOfRollingBackupsSpinnerSpin(string direction)
    {
        if (direction == "Increase")
        {
            if (AmountOfRollingBackups >= MAX_SPINNER) return;
            AmountOfRollingBackups++;
        }
        else if (direction == "Decrease")
        {
            if (AmountOfRollingBackups <= MIN_SPINNER) return;
            AmountOfRollingBackups--;
        }
        _amountOfRollingBackupsSubject.OnNext(DateTime.UtcNow);
    }

    private void OnAmountOfRollingBackupsChangedDebounced()
    {
        var set = _settingsService.Get();
        set.RollingBackupsAmount = AmountOfRollingBackups;
        _settingsService.Update(set);
        WeakReferenceMessenger.Default.Send(new RollingBackupAmountChangedMessage(AmountOfRollingBackups));
    }

    [RelayCommand]
    public void MinutesBetweenRollingBackupsSpinnerSpin(string direction)
    {
        if (direction == "Increase")
        {
            if (MinutesBetweenRollingBackups >= MAX_SPINNER) return;
            MinutesBetweenRollingBackups++;
        }
        else if (direction == "Decrease")
        {
            if (MinutesBetweenRollingBackups <= MIN_SPINNER) return;
            MinutesBetweenRollingBackups--;
        }
        _minutesBetweenRollingBackupsSubject.OnNext(DateTime.UtcNow);
    }

    private void OnMinutesBetweenRollingBackupsChangedDebounced()
    {
        var set = _settingsService.Get();
        set.MinutesBetweenRollingBackups = MinutesBetweenRollingBackups;
        _settingsService.Update(set);
        WeakReferenceMessenger.Default.Send(new MinutesBetweenRollingBackupsChangedMessage(MinutesBetweenRollingBackups));
    }
}
