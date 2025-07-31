using Avalonia.Controls;
using lib.remnant2.analyzer.SaveLocation;
using RemnantOverseer.Utilities;
using RemnantOverseer.Models;
using RemnantOverseer.Models.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RemnantOverseer.Services;
public class SettingsService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
    private readonly string _path;
    private readonly Task<Settings> _settings;

    public SettingsService(string path)
    {
        _path = path;
        _settings = Load(path);

        // XAML Designer support
        if (Design.IsDesignMode)
        {
            return; // TODO?
        }
    }

    public SettingsService() : this(GetDefaultSettingsPath()) {}

    // potentially move this elsewhere?
    private static string GetDefaultSettingsPath()
    {
# if REMNANTOVERSEER_USER_DIRECTORIES
        string userdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
        return Path.Combine(userdir, "remnant-two-overseer", "settings.json");
# else
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
# endif
    }

    public Settings Get()
    {
        return _settings.Result;
    }

    private static async Task<Settings> Load(string path)
    {
        ConfigData config = new();
        if (File.Exists(path))
        {
            try
            {
                string json = await File.ReadAllTextAsync(path);
                config = JsonSerializer.Deserialize<ConfigData>(json)!;
            }
            catch (Exception ex)
            {
                var message = NotificationStrings.ErrorWhenLoadingSettings + Environment.NewLine + ex.Message;
                WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(message));
                Log.Instance.Warning(message);
            }
        }
        return new Settings(config);
    }

    public async Task Sync()
    {
        // maybe reduce churn here by waiting for a bit, kinda like discard=async
        // also check for equality with _settings, to make sure we only write on a change
        // do that within the semaphore, that's the only place we update the settings after the initial load

        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(Get().Config, options: _options);
            string? dir = Path.GetDirectoryName(_path);
            // should never happen, just to make the compiler happy
            if(dir == null) throw new InvalidOperationException("Cannot determine settings path");
            await Task.Run(() => Directory.CreateDirectory(dir));
            await File.WriteAllTextAsync(_path, json);
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
