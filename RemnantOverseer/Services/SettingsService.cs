using CommunityToolkit.Mvvm.Messaging;
using lib.remnant2.analyzer;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services.Models;
using RemnantOverseer.Utilities;
using System;
using System.IO;
using System.Text.Json;

namespace RemnantOverseer.Services;
public class SettingsService
{
    private readonly object _lock = new object();
    private readonly string path = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private Settings _settings = new();
    private JsonSerializerOptions _options = new() { WriteIndented = true };

    public SettingsService()
    {
        // XAML Designer support
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            return; // TODO?
        }        
    }

    public void Initialize()
    {
        if (File.Exists(path))
        {
            // Considering making a toast for this and remaking the file. But I think crashing is more educational
            string json = File.ReadAllText(path);
            _settings = JsonSerializer.Deserialize<Settings>(json)!;
        }

        if (_settings.RollingBackupsAmount == 0 || _settings.MinutesBetweenRollingBackups == 0)
        {
            _settings.RollingBackupsAmount = 3;
            _settings.MinutesBetweenRollingBackups = 10;
        }

        if (_settings.SaveFilePath == null)
        {
            // Try to get a path
            try
            {
                _settings.SaveFilePath = Utils.GetSteamSavePath();
                // TODO: throw if dir doesnt exist?
            }
            catch
            {
                WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.DefaultLocationNotFound));
                return;
            }

            WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.DefaultLocationFound));
        }

        if (_settings.BackupsPath == null)
        {
            // TODO: Handle this better. Check directory
            //Directory.CreateDirectory(_settings.BackupsPath);
            _settings.BackupsPath = Path.Combine(_settings.SaveFilePath, "BackupsTest");
        }

        Update(_settings);
    }

    // Application is simple enough to allow client to read the whole config.
    // Could implement more granular approach later
    public Settings Get()
    {
        // Return a clone?
        return _settings;
    }

    public void Update(Settings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, options: _options);
            lock (_lock)
            {
                File.WriteAllText(path, json);
                _settings = settings;
            }
        }
        catch(Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.ErrorWhenUpdatingSettings + Environment.NewLine + ex.Message));
        }
    }
}
