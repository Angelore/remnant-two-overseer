using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services.Models;
using RemnantOverseer.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RemnantOverseer.Services;
internal class BackupService
{
    private bool _initialized;

    private bool _isRollingBackupEnabled;

    private byte _amountOfRollingBackups;

    private byte _backupIntervalMinutes;

    private string _pathToSave;

    private string _pathToBackups;

    private readonly string _rollingFileNamePattern = "Rolling.{0}.{1}"; // save active char as id or just guid?

    private readonly string _fileNamePattern = "Backup.{0}.{1}";

    private readonly string _backupSettingsFileName = "backups.json";
    private JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly object _lock = new object();

    private SettingsService _settingsService;
    private SaveDataService _saveDataService;

    private BackupSettings _backupSettings;

    private string BackupSettingsFilePath => Path.Combine(_pathToBackups, _backupSettingsFileName);

    public BackupService(SettingsService settingsService, SaveDataService saveDataService)
    {
        // XAML Designer support
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            return;
        }
        _settingsService = settingsService;
        _saveDataService = saveDataService;
    }

    public void Initialize()
    {
        if (_settingsService.Get().BackupsPath != null || _settingsService.Get().SaveFilePath != null)
        { // { throw new Exception("Settings service not initialized, can't init backup service"); }
            InitializeInternal();
        }
        else
        {
            _isRollingBackupEnabled = false;
            var set = _settingsService.Get();
            set.RollingBackupsEnabled = false;
            _settingsService.Update(set);
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.BackupServiceInInvalidState));
        }

        WeakReferenceMessenger.Default.Register<SaveFilePathChangedMessage>(this, async (r, m) => await SaveFilePathChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<BackupPathChangedMessage>(this, async (r, m) => await BackupPathChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<SaveFileChangedMessage>(this, async (r, m) => await SaveFileChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<RollingBackupsAmountChangedMessage>(this, async (r, m) => await RollingBackupsAmountChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<RollingBackupsEnabledChangedMessage>(this, async (r, m) => await RollingBackupsEnabledChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<MinutesBetweenRollingBackupsChangedMessage>(this, async (r, m) => await MinutesBetweenRollingBackupsChangedMessageHandler(m));
        // Add handler for manual save
        // Add handler for restore
    }

    private void InitializeInternal()
    {
        if (_settingsService.Get().BackupsPath != null || _settingsService.Get().SaveFilePath != null)
        {
            _pathToBackups = _settingsService.Get().BackupsPath!;
            _pathToSave = _settingsService.Get().SaveFilePath!;
            _amountOfRollingBackups = _settingsService.Get().RollingBackupsAmount;
            _backupIntervalMinutes = _settingsService.Get().MinutesBetweenRollingBackups;
            _isRollingBackupEnabled = _settingsService.Get().RollingBackupsEnabled;

            if (!Directory.Exists(_pathToBackups))
            {
                try
                {
                    Directory.CreateDirectory(_pathToBackups);
                    WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.BackupFolderCreated));
                }
                catch
                {
                    WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.MasterBackupFolderCantBeCreated));
                    return;
                }
            }

            if (!File.Exists(BackupSettingsFilePath))
            {
                // No previous db, create a new one
                _backupSettings = new BackupSettings();
                UpdateSettingsFile(_backupSettings);
            }

            // Considering making a toast for this and remaking the file. But I think crashing is more educational (part 2)
            string json = File.ReadAllText(BackupSettingsFilePath);
            _backupSettings = JsonSerializer.Deserialize<BackupSettings>(json)!;

            ValidateBackupFile(_backupSettings);

            _initialized = true;
        }
        else
        {
            _isRollingBackupEnabled = false;
            var set = _settingsService.Get();
            set.RollingBackupsEnabled = false;
            _settingsService.Update(set);
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.BackupServiceInInvalidState));

            _initialized = false;
        }
    }

    private bool IsGameRunning()
    {
        Process[] processes = Process.GetProcessesByName("Remnant2");
        if (processes.Length == 0)
        {
            return false;
        }
        return true;
    }

    private void ValidateBackupFile(BackupSettings settings)
    {
        var isRemovedBackupEntries = CheckEntriesAndRemoveInvalid(settings.BackupSettingsEntries);
        var isRemovedRollingEntries = CheckEntriesAndRemoveInvalid(settings.RollingBackupSettingsEntries);
        // TODO: Send message

        UpdateSettingsFile(settings);
    }

    private bool CheckEntriesAndRemoveInvalid(List<BackupSettingsEntry> entries)
    {
        var removedEntries = false;
        foreach (var entry in entries)
        {
            var tmpath = Path.Combine(_pathToBackups, entry.Name);
            if (string.IsNullOrEmpty(entry.Name) ||
                !Directory.Exists(tmpath) ||
                Directory.GetFiles(tmpath).Length == 0)
            {
                entries.Remove(entry);
                removedEntries = true;
            }
        }

        return removedEntries;
    }

    private void UpdateSettingsFile(BackupSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, options: _jsonOptions);
            lock (_lock)
            {
                File.WriteAllText(BackupSettingsFilePath, json);
                _backupSettings = settings;
            }
        }
        catch(Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.ErrorWhenUpdatingBackupSettings + Environment.NewLine + ex.Message));
        }
    }

    public async Task<bool> SaveManualBackup()
    {
        return await SaveBackup(BackupTypes.Regular);
    }

    private async Task<bool> SaveBackup(BackupTypes backupType)
    {
        if (!Directory.Exists(_pathToSave))
        {
            WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.SaveFolderDoesntExist));
            return false;
        }
        if (!Directory.Exists(_pathToBackups))
        {
            WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.BackupFolderDoesntExist));
            return false;
        }

        var namePattern = backupType switch
        {
            BackupTypes.Regular => _fileNamePattern,
            BackupTypes.Rolling => _rollingFileNamePattern,
            _ => throw new NotImplementedException(),
        };
        var saveDescription = await GetSaveDataDescription(true);
        var saveDescriptionFull = await GetSaveDataDescription(false);

        if (saveDescription == null || saveDescriptionFull == null)
        {
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.BackupDatasetIsEmpty));
            return false;
        }

        var folderName = string.Format(namePattern, saveDescription, DateTime.Now.Ticks / 10000);

        try
        {
            var dirInfo = Directory.CreateDirectory(Path.Combine(_pathToBackups, folderName));
        }
        catch
        {
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.BackupFolderCantBeCreated));
            return false;
        }

        try
        {
            foreach (string file in Directory.EnumerateFiles(_pathToSave, "*.sav"))
            {
                File.Copy(file, Path.Combine(Path.Combine(_pathToBackups, folderName), Path.GetFileName(file)), true);
            }
        }
        catch
        {
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.FileCopyError));
        }

        if (backupType == BackupTypes.Regular)
        {
            _backupSettings.BackupSettingsEntries.Add(new BackupSettingsEntry()
            {
                BackupDate = DateTime.Now,
                Description = saveDescriptionFull,
                Name = folderName,
                Type = BackupTypes.Regular
            });
            WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(string.Format(NotificationStrings.ManualBackupSuccessful, folderName)));
        }
        return true;
    }

    public async Task<string?> GetSaveDataDescription(bool isCompact)
    {
        var data = await _saveDataService.GetSaveData();
        if (data == null)
            return null;

        var activeCharacter = data.Characters[data.ActiveCharacterIndex];
        var playtime = activeCharacter.Save.Playtime ?? TimeSpan.Zero;

        if (isCompact)
        {
            return
                activeCharacter.Profile.Archetype +
                (string.IsNullOrEmpty(activeCharacter.Profile.SecondaryArchetype) ? "" : activeCharacter.Profile.SecondaryArchetype) +
                activeCharacter.Profile.ItemLevel;
        }
        else
        {
            return
                activeCharacter.Profile.Archetype + "/" +
                (string.IsNullOrEmpty(activeCharacter.Profile.SecondaryArchetype) ? "" : activeCharacter.Profile.SecondaryArchetype) +
                "[" + activeCharacter.Profile.ItemLevel + "]" +
                " Playtime: " + (int)playtime.TotalHours + playtime.ToString(@"\:mm\:ss");
        }
    }

    private async Task SaveFilePathChangedMessageHandler(SaveFilePathChangedMessage m)
    {
        if (!_initialized)
        {
            InitializeInternal();
            return;
        };
        
        _pathToSave = m.Value;
    }

    private async Task BackupPathChangedMessageHandler(BackupPathChangedMessage m)
    {
        if (!_initialized)
        {
            InitializeInternal();
            return;
        };

        // TODO: Move backup settings file and backups?
    }

    private async Task SaveFileChangedMessageHandler(SaveFileChangedMessage m)
    {
        if (!_initialized) return;
        throw new NotImplementedException();
    }

    private async Task RollingBackupsAmountChangedMessageHandler(RollingBackupsAmountChangedMessage m)
    {
        if (!_initialized) return;
        throw new NotImplementedException();
    }

    private async Task RollingBackupsEnabledChangedMessageHandler(RollingBackupsEnabledChangedMessage m)
    {
        throw new NotImplementedException();
    }

    private async Task MinutesBetweenRollingBackupsChangedMessageHandler(MinutesBetweenRollingBackupsChangedMessage m)
    {
        throw new NotImplementedException();
    }
}
