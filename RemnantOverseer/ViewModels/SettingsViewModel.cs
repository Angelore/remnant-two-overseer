﻿using Avalonia.Controls;
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
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;
public partial class SettingsViewModel: ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly SaveDataService _saveDataService;

    [ObservableProperty]
    private string? _filePath;

    public SettingsViewModel(SettingsService settingsService, SaveDataService saveDataService)
    {
        _settingsService = settingsService;
        _saveDataService = saveDataService;
        FilePath = _settingsService.Get()?.SaveFilePath ?? null;

        if(Design.IsDesignMode)
        {
            FilePath = @"C:\Remnant\Remnant 2: I'm Beginning to Remn";
        }
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
                
                settings.SaveFilePath = newPath;
                _settingsService.Update(settings);
                FilePath = newPath;
                WeakReferenceMessenger.Default.Send(new SaveFilePathChangedMessage(FilePath!));
                WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.SaveFileLocationChanged));
            }
        }
    }

    [RelayCommand]
    public async Task OpenLog()
    {
        var topLevel = FileDialogManager.GetTopLevelForContext(this);
        if (topLevel != null)
        {
            try
            {
                await topLevel.Launcher.LaunchFileInfoAsync(new FileInfo(Log.LogFilePath));
            }
            catch { }
        }
    }

    [RelayCommand]
    public async Task DumpPlayerInfo()
    {
        await Task.Run(_saveDataService.ReportPlayerInfo);
    }

    [RelayCommand]
    public async Task ExportSave()
    {
        await Task.Run(_saveDataService.ExportSave);
    }

    public static FilePickerFileType Saves { get; } = new("Remnant 2 save files")
    {
        Patterns = ["profile.sav", "containers.index"],
    };
}
