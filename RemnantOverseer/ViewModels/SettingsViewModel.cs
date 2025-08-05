using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using RemnantOverseer.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;
public partial class SettingsViewModel : ViewModelBase
{
    private readonly SaveDataService _saveDataService;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _enableVersionCheck;

    [ObservableProperty]
    private bool _showTips;

    [ObservableProperty]
    private bool _showToolkitLinks;

    public SettingsViewModel(SettingsService settingsService, SaveDataService saveDataService)
    {
        _settingsService = settingsService;
        _saveDataService = saveDataService;
        var settings = _settingsService.Get();
        _filePath = settings.SaveFilePath;
        _showTips = !settings.HideTips;
        _showToolkitLinks = !settings.HideToolkitLinks;
        _enableVersionCheck = !settings.DisableVersionCheck;

        if (Design.IsDesignMode)
        {
            FilePath = @"C:\Remnant\Remnant 2: I'm Beginning to Remn";
        }
    }

    [RelayCommand]
    public async Task OpenFile()
    {
        var topLevel = FileDialogManager.GetTopLevelForContext(this);
        if (topLevel == null) return;

        var storageFiles = await topLevel.StorageProvider.OpenFilePickerAsync(
                        new FilePickerOpenOptions()
                        {
                            FileTypeFilter = [Saves],
                            AllowMultiple = false,
                            Title = "Select the profile file. Can be either 'profile.sav' or 'containers.index'"
                        });

        if (storageFiles.Count > 0)
        {
            var selectedFile = storageFiles[0];
            var settings = _settingsService.Get();
            var localPath = selectedFile.TryGetLocalPath()!;
            if (Path.GetExtension(localPath).Equals(".index"))
            {
                // Gamepass need one extra jump
                //localPath = new DirectoryInfo(localPath).Parent!.Parent!.FullName;
                localPath = Path.GetDirectoryName(localPath);
            }
            var newPath = Path.GetDirectoryName(localPath);

            if (newPath == settings.SaveFilePath) return;

            FilePath = newPath;
            settings.SaveFilePath = newPath;
            await _settingsService.Sync();
            WeakReferenceMessenger.Default.Send(new SaveFilePathChangedMessage(FilePath!));
            WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.SaveFileLocationChanged));
        }
    }

    [RelayCommand]
    public async Task OpenLog()
    {
        var topLevel = FileDialogManager.GetTopLevelForContext(this);
        if (topLevel == null) return;

        try
        {
            await topLevel.Launcher.LaunchFileInfoAsync(new FileInfo(Log.GetLogFilePath()));
        }
        catch { }
    }

    [RelayCommand]
    public async Task DumpPlayerInfo()
    {
        await Task.Run(_saveDataService.ReportPlayerInfo);
    }

    [RelayCommand]
    public async Task ExportSave()
    {
        var topLevel = FileDialogManager.GetTopLevelForContext(this);
        if (topLevel == null) return;

        var documents = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        var storageDir = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions()
            {
                Title = "Select a folder to export save data to",
                SuggestedStartLocation = documents,
                SuggestedFileName = $"Remnant2_export_{DateTime.Now:yyyy-MM-dd}",
                AllowMultiple = false,
            });
        if (storageDir.Count == 0) return;

        await Task.Run(() => _saveDataService.ExportSave(storageDir[0].TryGetLocalPath()));
    }

    partial void OnEnableVersionCheckChanged(bool value)
    {
        Task.Run(async () =>
        {
            _settingsService.Get().DisableVersionCheck = !value;
            await _settingsService.Sync();
            WeakReferenceMessenger.Default.Send(new DisableVersionCheckChangedMessage(!value));
        });
    }

    partial void OnShowTipsChanged(bool value)
    {
        Task.Run(async () =>
        {
            _settingsService.Get().DisableVersionCheck = !value;
            await _settingsService.Sync();
            WeakReferenceMessenger.Default.Send(new HideTipsChangedMessage(!value));
        });
    }

    partial void OnShowToolkitLinksChanged(bool value)
    {
        Task.Run(async () =>
        {
            _settingsService.Get().HideToolkitLinks = !value;
            await _settingsService.Sync();
            WeakReferenceMessenger.Default.Send(new HideToolkitLinksChangedMessage(!value));
        });
    }

    public static FilePickerFileType Saves { get; } = new("Remnant 2 save files")
    {
        Patterns = ["profile.sav", "containers.index"],
    };
}
