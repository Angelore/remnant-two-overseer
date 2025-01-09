using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using lib.remnant2.analyzer;
using lib.remnant2.analyzer.Model;
using RemnantOverseer.Models.Messages;
using System;
using System.IO;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using RemnantOverseer.Utilities;

namespace RemnantOverseer.Services;
public class SaveDataService
{
    private readonly SettingsService _settingsService;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    Subject<DateTime> _fileUpdateSubject = new Subject<DateTime>();
    private Dataset? _dataset;
    private int _lastCharacterCount = 0;

    private string? FilePath => _settingsService.Get().SaveFilePath;

    private static readonly FileSystemWatcher FileWatcher = new()
    {
        Filter = "profile.sav",
    };

    public SaveDataService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        FileWatcher.Changed += OnSaveFileChanged;
        FileWatcher.Created += OnSaveFileChanged;
        FileWatcher.Deleted += OnSaveFileChanged;

        // File watcher often raises multiple events for one file update
        _fileUpdateSubject.Throttle(TimeSpan.FromSeconds(1)).Subscribe(async events => await OnSaveFileChangedDebounced());
        WeakReferenceMessenger.Default.Register<SaveFilePathChangedMessage>(this, async (r, m) => await SaveFilePathChangedMessageHandler(m));
    }

    public async Task<Dataset?> GetSaveData()
    {
        if (FilePath is null)
        {
            WeakReferenceMessenger.Default.Send(new DatasetIsNullMessage());
            return null;
        }

        // TODO: Add timeout?
        await _semaphore.WaitAsync();
        try
        {
            if (_dataset == null)
            {
                _dataset = await Task.Run(() => Analyzer.Analyze(FilePath));
                _lastCharacterCount = _dataset.Characters.Count;
            }
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new NotificationErrorMessage($"{NotificationStrings.SaveFileParsingError}{Environment.NewLine}{ex.Message}"));
        }
        finally
        {
            _semaphore.Release();
        }

        if (_dataset == null)
            WeakReferenceMessenger.Default.Send(new DatasetIsNullMessage());
        else
            WeakReferenceMessenger.Default.Send(new DatasetParsedMessage());
        return _dataset;
    }

    public void Reset()
    {
        _dataset = null;
    }

    public bool StartWatching()
    {
        if (FilePath is null) return false;

        if (Directory.Exists(FilePath))
        {
            FileWatcher.Path = FilePath;
            FileWatcher.EnableRaisingEvents = true;
            return true;
        }
        else
        {
            FileWatcher.EnableRaisingEvents = false;
            WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.FileWatcherFolderNotFound));
            return false;
        }
    }
    public void PauseWatching()
    {
        FileWatcher.EnableRaisingEvents = false;
    }
    public void ResumeWatching()
    {
        if (FileWatcher.Path == null) return;
        FileWatcher.EnableRaisingEvents = true;
    }

    private void OnSaveFileChanged(object sender, FileSystemEventArgs e)
    {
        _fileUpdateSubject.OnNext(DateTime.UtcNow);
    }

    private async Task OnSaveFileChangedDebounced()
    {
        _dataset = await Task.Run(() => Analyzer.Analyze(FilePath));
        // If the number of character changed, we can't rely on previous index anymore. There is no way to uniquely id  characters, so we will just reset
        var countChanged = _dataset.Characters.Count > _lastCharacterCount;
        _lastCharacterCount = _dataset.Characters.Count;
        WeakReferenceMessenger.Default.Send(new SaveFileChangedMessage(countChanged));
    }

    private async Task SaveFilePathChangedMessageHandler(SaveFilePathChangedMessage message)
    {
        PauseWatching();
        // TODO: Change to reset + getsavedata. Do after backups are done to not mess with it for now.
        _dataset = await Task.Run(() => Analyzer.Analyze(FilePath));
        _lastCharacterCount = _dataset.Characters.Count;
        StartWatching();
        WeakReferenceMessenger.Default.Send(new SaveFileChangedMessage(true));
    }

    #region For debug only
    internal async Task ExportAsJson()
    {
        if (FilePath is null) throw new ArgumentNullException("File path not set");
        await Task.Run(() => Exporter.Export(FilePath!, FilePath, false, false, true));
    }

    internal void ParseSave()
    {
        if (FilePath is null) throw new ArgumentNullException("File path not set");

        var saves = lib.remnant2.analyzer.Analyzer.GetProfileStrings(FilePath);
        Debug.WriteLine(saves);

        var data = lib.remnant2.analyzer.Analyzer.Analyze(FilePath);
        Debug.WriteLine(data);
    }
    #endregion
}
