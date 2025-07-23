using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using lib.remnant2.analyzer.SaveLocation;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Utilities;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RemnantOverseer.Services;
public class SettingsService
{
    private readonly object _lock = new object();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private Settings _settings = new();
    private JsonSerializerOptions _options = new() { WriteIndented = true };

    public SettingsService()
    {
        // XAML Designer support
        if (Design.IsDesignMode)
        {
            return; // TODO?
        }        
    }

    private string GetSettingsPath()
    {
# if REMNANTOVERSEER_USER_DIRECTORIES
        string userdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
        return Path.Combine(userdir, "remnant-two-overseer", "settings.json");
# else
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
# endif
    }

    public void Initialize()
    {
        string path = SettingsPath();
        if (File.Exists(path))
        {
            // Considering making a toast for this and remaking the file. But I think crashing is more educational
            string json = File.ReadAllText(path);
            _settings = JsonSerializer.Deserialize<Settings>(json)!;
        }

        if (_settings.SaveFilePath == null)
        {
            // Try to get a path
            try
            {
                _settings.SaveFilePath = SaveUtils.GetSaveFolder();
                Update(_settings);
                WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.DefaultLocationFound));
                Log.Instance.Information(NotificationStrings.DefaultLocationFound);
            }
            catch
            {
                WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.DefaultLocationNotFound));
                Log.Instance.Warning(NotificationStrings.DefaultLocationNotFound);
                return;
            }
        }
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
        if (settings.SaveFilePath == null)
        {
            ;
        }
        try
        {
            var json = JsonSerializer.Serialize(settings, options: _options);
            lock (_lock)
            {
                string path = SettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
                _settings = settings;
            }
        }
        catch (Exception ex)
        {
            var message = NotificationStrings.ErrorWhenUpdatingSettings + Environment.NewLine + ex.Message;
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(message));
            Log.Instance.Warning(message);
        }
    }

    public async Task UpdateAsync(Settings settings)
    {
        if (settings.SaveFilePath == null)
        {
            ;
        }
        await _semaphore.WaitAsync();
        try
        {
            string path = SettingsPath();
            var json = JsonSerializer.Serialize(settings, options: _options);
            await Task.Run(() => Directory.CreateDirectory(Path.GetDirectoryName(path)));
            await File.WriteAllTextAsync(path, json);
            _settings = settings;
        }
        catch (Exception ex)
        {
            var message = NotificationStrings.ErrorWhenUpdatingSettings + Environment.NewLine + ex.Message;
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(message));
            Log.Instance.Warning(message);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
