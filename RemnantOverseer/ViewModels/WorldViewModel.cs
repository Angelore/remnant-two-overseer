using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;

public partial class WorldViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly SaveDataService _saveDataService;
    private readonly StateService _stateService;
    private MappedZones _mappedZones = new();
    private int _selectedCharacterIndex = -1;
    private readonly Subject<string?> _filterTextSubject = new Subject<string?>();

    [ObservableProperty]
    private ObservableCollection<Zone> _filteredZones = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isCampaignSelected = true; // TODO: add disabling when no adventure

    [ObservableProperty]
    private bool _isGlobalExpandOn = true;

    [ObservableProperty]
    private bool _hideDuplicates;

    [ObservableProperty]
    private bool _hideLootedItems;

    [ObservableProperty]
    private bool _hideMissingPrerequisiteItems;

    [ObservableProperty]
    private bool _hideHasRequiredMaterialItems;

    [ObservableProperty]
    private bool _isNerudFilterChecked = false;

    [ObservableProperty]
    private bool _isYaeshaFilterChecked = false;

    [ObservableProperty]
    private bool _isLosomnFilterChecked = false;

    [ObservableProperty]
    private string? _filterText = null;

    [ObservableProperty]
    private ThaenTree? _thaenTree;

    [ObservableProperty]
    private List<string> _completedQuests = [];

    private BloodmoonInfo? _bloodmoonInfoCampaign = null;
    private BloodmoonInfo? _bloodmoonInfoAdventure = null;

    // Drives icon visibility only; the tooltip text is built on demand in GetBloodmoonTooltip.
    [ObservableProperty]
    private float? _bloodmoonChance = null;

    private BloodmoonInfo? CurrentBloodmoonInfo => IsCampaignSelected ? _bloodmoonInfoCampaign : _bloodmoonInfoAdventure;

    // Non-null placeholder, so Avalonia wires it up
    public string BloodmoonTooltip => string.Empty;

    // Built on hover so the time-relative text and the optional debug lines reflect the
    // moment the tooltip is shown.
    public string GetBloodmoonTooltip(bool debug)
    {
        var info = CurrentBloodmoonInfo;
        if (info is null) return LocalizationService.Get("Common_Unknown");

        var now = DateTime.UtcNow;
        var chance = Math.Round(info.CurrentChance, 2, MidpointRounding.AwayFromZero);
        var sinceTriggered = now - info.LastTriggeredTime;
        var sinceChecked = now - info.LastCheckTime;

        string main;
        if (sinceTriggered >= TimeSpan.Zero && sinceTriggered <= TimeSpan.FromMinutes(20))
        {
            var minutes = (int)Math.Ceiling(((info.LastTriggeredTime + TimeSpan.FromMinutes(20)) - now).TotalMinutes);
            main = LocalizationService.Format("World_BloodmoonActive", minutes);
        }
        else if (sinceTriggered >= TimeSpan.Zero && sinceTriggered <= TimeSpan.FromMinutes(80))
        {
            var minutes = (int)Math.Ceiling(((info.LastTriggeredTime + TimeSpan.FromMinutes(80)) - now).TotalMinutes);
            main = LocalizationService.Format("World_BloodmoonCooldown", minutes);
        }
        else if (info.LastTriggeredTime == info.LastCheckTime)
        {
            main = LocalizationService.Get("World_BloodmoonCooldownReady");
        }
        else if (sinceChecked >= TimeSpan.Zero && sinceChecked <= TimeSpan.FromMinutes(6))
        {
            var minutes = (int)Math.Ceiling(((info.LastCheckTime + TimeSpan.FromMinutes(6)) - now).TotalMinutes);
            main = LocalizationService.Format("World_BloodmoonChanceNextCheck", chance, minutes);
        }
        else
        {
            main = LocalizationService.Format("World_BloodmoonChanceNextReady", chance);
        }

        if (!debug) return main;

        var sb = new StringBuilder();
        sb.AppendLine(main);
        sb.AppendLine($"Current Chance: {info.CurrentChance}");
        sb.AppendLine($"Last Triggered Time: {info.LastTriggeredTime.ToLocalTime()}");
        sb.AppendLine($"Last Check Time: {info.LastCheckTime.ToLocalTime()}");
        sb.Append($"Zone Load Count: {info.ZoneLoadCount}");
        return sb.ToString();
    }

    [ObservableProperty]
    private bool _hideTips;

    [ObservableProperty]
    private bool _hideToolkitLinks;

    public WorldViewModel(SettingsService settingsService, SaveDataService saveDataService, StateService stateService)
    {
        _settingsService = settingsService;
        var settings = _settingsService.Get();
        HideDuplicates = settings.HideDuplicates;
        HideLootedItems = settings.HideLootedItems;
        HideMissingPrerequisiteItems = settings.HideMissingPrerequisiteItems;
        HideHasRequiredMaterialItems = settings.HideHasRequiredMaterialItems;
        HideTips = settings.HideTips;
        HideToolkitLinks = settings.HideToolkitLinks;
        _saveDataService = saveDataService;
        _stateService = stateService;
        _filterTextSubject
          .Throttle(TimeSpan.FromMilliseconds(400))
          .Subscribe(OnFilterTextChangedDebounced);
    }

    public void OnViewLoaded()
    {
        if (IsInitialized) { return; }

        Task.Run(async () => {
            if (_stateService.SelectedCharacterIndex != null)
            {
                _selectedCharacterIndex = _stateService.SelectedCharacterIndex.Value;
                await ReadSave(false, true);
            }
            else
            {
                await ReadSave(true, true);
            }
            await ReadSave(true, true);
            IsActive = true;
            IsInitialized = true;
        });
    }

    [RelayCommand]
    private void ExpandTreeNodes()
    {
        IsGlobalExpandOn = !IsGlobalExpandOn;
    }

    #region Filtering
    // What?
    // https://devblogs.microsoft.com/ifdef-windows/announcing-net-community-toolkit-v8-0-0-preview-3/#partial-property-changed-methods
    partial void OnIsCampaignSelectedChanged(bool value)
    {
        ApplyFilter();
        BloodmoonChance = CurrentBloodmoonInfo is not null ? (float)CurrentBloodmoonInfo.CurrentChance : null;
    }

    partial void OnHideDuplicatesChanged(bool value)
    {
        ApplyFilter();
        Task.Run(async () =>
        {
            _settingsService.Get().HideDuplicates = value;
            await _settingsService.Sync();
        });
    }

    // Additional filters
    partial void OnHideLootedItemsChanged(bool value)
    {
        ApplyFilter();
        Task.Run(async () =>
        {
            _settingsService.Get().HideLootedItems = value;
            await _settingsService.Sync();
        });
    }

    partial void OnHideMissingPrerequisiteItemsChanged(bool value)
    {
        ApplyFilter();
        Task.Run(async () =>
        {
            _settingsService.Get().HideMissingPrerequisiteItems = value;
            await _settingsService.Sync();
        });
    }

    partial void OnHideHasRequiredMaterialItemsChanged(bool value)
    {
        ApplyFilter();
        Task.Run(async () =>
        {
            _settingsService.Get().HideHasRequiredMaterialItems = value;
            await _settingsService.Sync();
        });
    }
    // ~Additional filters

    partial void OnFilterTextChanged(string? value)
    {
        _filterTextSubject.OnNext(value);
    }

    private void OnFilterTextChangedDebounced(string? value)
    {
        ApplyFilter(value);
    }

    [RelayCommand]
    public void NerudFilterToggled()
    {
        if (IsNerudFilterChecked)
        {
            IsYaeshaFilterChecked = false;
            IsLosomnFilterChecked = false;
        }
        ApplyFilter();
    }

    [RelayCommand]
    public void YaeshaFilterToggled()
    {
        if (IsYaeshaFilterChecked)
        {
            IsNerudFilterChecked = false;
            IsLosomnFilterChecked = false;
        }
        ApplyFilter();
    }

    [RelayCommand]
    public void LosomnFilterToggled()
    {
        if (IsLosomnFilterChecked)
        {
            IsYaeshaFilterChecked = false;
            IsNerudFilterChecked = false;
        }
        ApplyFilter();
    }

    [RelayCommand]
    public void ResetFilters()
    {
        ResetLocationToggles();
        ResetAdditionalFilters();
        if (FilterText == null) ApplyFilter(); // If there is no filtertext but toggles were set, still need to filter
        FilterText = null;
    }

    private void ApplyFilter()
    {
        ApplyFilter(FilterText);
    }

    private void ApplyFilter(string? value)
    {
        var tempZones = IsCampaignSelected ? _mappedZones.CampaignZoneList : _mappedZones.AdventureZoneList;
        var tempFilteredZones = new List<Zone>();
        foreach (var zone in tempZones)
        {
            // Toggles only applicable to campaign
            if (IsCampaignSelected)
            {
                if (IsNerudFilterChecked && zone.CanonicalName != LocationStrings.Nerud) continue;
                if (IsYaeshaFilterChecked && zone.CanonicalName != LocationStrings.Yaesha) continue;
                if (IsLosomnFilterChecked && zone.CanonicalName != LocationStrings.Losomn) continue;
            }

            var tempZone = zone.ShallowCopy();
            tempZone.Locations = [];

            foreach (var location in zone.Locations)
            {
                var tempLocation = location.ShallowCopy();
                tempLocation.Items = [];

                // Add more processing if necessary. Remove special characters?
                IEnumerable<Item> tempItemsQuery = [];
                if (!string.IsNullOrEmpty(value))
                {
                    tempItemsQuery = location.Items.Where(i => i.Name.Contains(value, StringComparison.OrdinalIgnoreCase) || i.OriginName.Contains(value, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    tempItemsQuery = [..location.Items];
                }

                if (HideDuplicates)
                {
                    tempItemsQuery = tempItemsQuery.Where(i => !i.IsDuplicate);
                }
                if (HideLootedItems)
                {
                    tempItemsQuery = tempItemsQuery.Where(i => !i.IsLooted);
                }
                if (HideMissingPrerequisiteItems)
                {
                    tempItemsQuery = tempItemsQuery.Where(i => !i.IsPrerequisiteMissing);
                }
                if (HideHasRequiredMaterialItems)
                {
                    tempItemsQuery = tempItemsQuery.Where(i => !i.HasRequiredMaterial);
                }

                var tempItems = tempItemsQuery.ToList();
                if (tempItems.Count != 0) { tempLocation.Items = tempItems; tempZone.Locations.Add(tempLocation); }
            }
            if (tempZone.Locations.Count != 0) { tempFilteredZones.Add(tempZone); }
        }

        FilteredZones = new(tempFilteredZones);
    }
    #endregion Filtering

    // TODO: Look into skipping updates if character index doesn't match and reset is false?
    // Need to think about it, feel like it's a bad idea
    private async Task ReadSave(bool resetActiveCharacter, bool resetCampaignToggle)
    {
        IsLoading = true;

        var dataset = await _saveDataService.GetSaveData();
        if (dataset == null)
        {
            IsLoading = false;
            return;
        }

#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field. Call private field to avoid filtering on every assignment
        if (resetActiveCharacter)
        {
            _selectedCharacterIndex = DatasetMapper.GetActiveCharacterIndex(dataset);
            ResetLocationToggles();
            _filterText = null;
            OnPropertyChanged(nameof(FilterText));
        }

        _mappedZones = DatasetMapper.MapCharacterToZones(dataset.Characters[_selectedCharacterIndex]);
        if (resetCampaignToggle)
        {
            if (dataset.Characters[_selectedCharacterIndex].ActiveWorldSlot == lib.remnant2.analyzer.Enums.WorldSlot.Campaign)
            {
                _isCampaignSelected = true;
                OnPropertyChanged(nameof(IsCampaignSelected));
            }
            else
            {
                _isCampaignSelected = false;
                OnPropertyChanged(nameof(IsCampaignSelected));
            }
        }
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field

        ThaenTree = DatasetMapper.MapThaenTree(dataset.Characters[_selectedCharacterIndex]);
        CompletedQuests = dataset.Characters[_selectedCharacterIndex].Save.QuestCompletedLog;
        _bloodmoonInfoCampaign = DatasetMapper.GetBloodmoonInfo(dataset.Characters[_selectedCharacterIndex].Save.Campaign);
        _bloodmoonInfoAdventure = DatasetMapper.GetBloodmoonInfo(dataset.Characters[_selectedCharacterIndex].Save.Adventure);
        BloodmoonChance = CurrentBloodmoonInfo is not null ? (float)CurrentBloodmoonInfo.CurrentChance : null;

        ApplyFilter();

        IsLoading = false;
    }

    private async Task CharacterUpdatedHandler(int characterIndex)
    {
        _selectedCharacterIndex = characterIndex;
        await ReadSave(false, true);
    }

    private async Task SaveFileChangedHandler(bool characterCountChanged)
    {
        if (characterCountChanged)
        {
            await ReadSave(true, true);
        }
        else
        {
            await ReadSave(false, false);
        }
    }

    private void ResetLocationToggles()
    {
        IsNerudFilterChecked = false;
        IsYaeshaFilterChecked = false;
        IsLosomnFilterChecked = false;
    }

    // Updating the file three times in a row is... le bad? Maybe.
    private void ResetAdditionalFilters()
    {
        HideLootedItems = false;
        HideMissingPrerequisiteItems = false;
        HideHasRequiredMaterialItems = false;
    }

    #region Messages
    protected override void OnActivated()
    {
        Messenger.Register<WorldViewModel, CharacterSelectChangedMessage>(this, (r, m) => {
            IsLoading = true; // Look into it later, sometimes task starts just a moment too late and the old stuff still can be seen
            Task.Run(async () => await CharacterUpdatedHandler(m.Value));
        });

        Messenger.Register<WorldViewModel, SaveFileChangedMessage>(this, (r, m) => {
            IsLoading = true;
            Task.Run(async () => await SaveFileChangedHandler(m.CharacterCountChanged));
        });

        Messenger.Register<WorldViewModel, HideTipsChangedMessage>(this, (r, m) => {
            HideTips = m.Value;
        });

        Messenger.Register<WorldViewModel, HideToolkitLinksChangedMessage>(this, (r, m) => {
            HideToolkitLinks = m.Value;
        });

        Messenger.Register<WorldViewModel, CultureChangedMessage>(this, (r, m) => {
            r.ApplyFilter();
            // Bloodmoon tooltip text is rebuilt on hover, so it always reflects the current culture.
            r.RefreshLocalizedTreeProperties();
        });
    }
    #endregion Messages

    private void RefreshLocalizedTreeProperties()
    {
        foreach (var zone in FilteredZones)
        {
            foreach (var location in zone.Locations)
            {
                location.RefreshLocalizedProperties();

                foreach (var item in location.Items)
                {
                    item.RefreshLocalizedProperties();
                }
            }
        }
    }
}
