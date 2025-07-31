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
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;

public partial class WorldViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly SaveDataService _saveDataService;
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
    private bool _hideMissingPrerequisiteItems ;

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

    private float? _bloodmoonChanceCampaign = null;
    private float? _bloodmoonChanceAdventure = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BloodmoonChanceString))]
    private float? _bloodmoonChance = null;

    public string BloodmoonChanceString => BloodmoonChance.HasValue ? $"Blood moon chance: {BloodmoonChance.Value}%" : "Unknown";

    [ObservableProperty]
    private bool _hideTips;

    [ObservableProperty]
    private bool _hideToolkitLinks;

    public WorldViewModel(SettingsService settingsService, SaveDataService saveDataService)
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
        _filterTextSubject
          .Throttle(TimeSpan.FromMilliseconds(400))
          .Subscribe(OnFilterTextChangedDebounced);
    }

    public void OnViewLoaded()
    {
        if (IsInitialized) { return; }

        Task.Run(async () => { await ReadSave(true, true); IsActive = true; IsInitialized = true; });
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
        BloodmoonChance = IsCampaignSelected ? _bloodmoonChanceCampaign : _bloodmoonChanceAdventure;
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
                if (IsNerudFilterChecked && zone.Name != LocationStrings.Nerud) continue;
                if (IsYaeshaFilterChecked && zone.Name != LocationStrings.Yaesha) continue;
                if (IsLosomnFilterChecked && zone.Name != LocationStrings.Losomn) continue;
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
    private async Task ReadSave(bool doResetActiveCharacter, bool doResetCampaignToggle)
    {
        IsLoading = true;

        var dataset = await _saveDataService.GetSaveData();
        if (dataset == null)
        {
            IsLoading = false;
            return;
        }

#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field. Call private field to avoid filtering on every assignment
        if (doResetActiveCharacter)
        {
            _selectedCharacterIndex = DatasetMapper.GetActiveCharacterIndex(dataset);
            ResetLocationToggles();
            _filterText = null;
            OnPropertyChanged(nameof(FilterText));
        }

        _mappedZones = DatasetMapper.MapCharacterToZones(dataset.Characters[_selectedCharacterIndex]);
        if (doResetCampaignToggle)
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
        _bloodmoonChanceCampaign = DatasetMapper.GetBloodmoonChance(dataset.Characters[_selectedCharacterIndex].Save.Campaign);
        _bloodmoonChanceAdventure = DatasetMapper.GetBloodmoonChance(dataset.Characters[_selectedCharacterIndex].Save.Adventure);
        BloodmoonChance = IsCampaignSelected ? _bloodmoonChanceCampaign : _bloodmoonChanceAdventure;

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
    }
    #endregion Messages
}
