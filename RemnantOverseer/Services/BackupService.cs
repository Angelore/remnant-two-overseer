using CommunityToolkit.Mvvm.Messaging;
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

    private string BackupSettingsFileName => Path.Combine(_pathToBackups, _backupSettingsFileName);

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
        if (_settingsService.Get().BackupPath == null || _settingsService.Get().SaveFilePath == null) { throw new Exception("Settings service not initialized, can't init backup service"); }
        _pathToBackups = _settingsService.Get().BackupPath!;
        _pathToSave = _settingsService.Get().SaveFilePath!;
        _amountOfRollingBackups = _settingsService.Get().RollingBackupAmount; // TODO: Allow to disable rolling backups completely?

        if (!Directory.Exists(_pathToBackups))
        {
            Directory.CreateDirectory(_pathToBackups);
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.DefaultLocationNotFound));
            return;
        }

        if(!File.Exists(BackupSettingsFileName))
        {
            // No previous db, create a new one
            _backupSettings = new BackupSettings();
            UpdateSettingsFile(_backupSettings);
            return;
        }

        // Considering making a toast for this and remaking the file. But I think crashing is more educational (part 2)
        string json = File.ReadAllText(BackupSettingsFileName);
        _backupSettings = JsonSerializer.Deserialize<BackupSettings>(json)!;

        ValidateSettings(_backupSettings);

        WeakReferenceMessenger.Default.Register<SaveFileChangedMessage>(this, async (r, m) => await SaveFileChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<SaveFilePathChangedMessage>(this, async (r, m) => await SaveFilePathChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<BackupPathChangedMessage>(this, async (r, m) => await BackupPathChangedMessageHandler(m));
        WeakReferenceMessenger.Default.Register<RollingBackupAmountChangedMessage>(this, async (r, m) => await RollingBackupAmountChangedMessageHandler(m));
        // Add handler for manual save
        // Add handler for restore
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

    private void ValidateSettings(BackupSettings settings)
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
        var json = JsonSerializer.Serialize(settings, options: _jsonOptions);
        lock (_lock)
        {
            File.WriteAllText(BackupSettingsFileName, json);
            _backupSettings = settings;
        }
    }

    private async Task SaveBackup()
    {
        if (!Directory.Exists(_pathToSave))
        {
            // TODO: Send message
            return;
        }
        if (!Directory.Exists(_pathToBackups))
        {
            // TODO: send message
            return;
        }

        var dirInfo = Directory.CreateDirectory(BackupSettingsFileName);
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

    private async Task SaveFileChangedMessageHandler(SaveFileChangedMessage m)
    {
        throw new NotImplementedException();
    }

    private async Task SaveFilePathChangedMessageHandler(SaveFilePathChangedMessage m)
    {
        throw new NotImplementedException();
    }

    private async Task RollingBackupAmountChangedMessageHandler(RollingBackupAmountChangedMessage m)
    {
        throw new NotImplementedException();
    }

    private async Task BackupPathChangedMessageHandler(BackupPathChangedMessage m)
    {
        throw new NotImplementedException();
    }
}
