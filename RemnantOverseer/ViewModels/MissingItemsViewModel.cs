using RemnantOverseer.Models.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System.Collections.ObjectModel;
using RemnantOverseer.Models;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace RemnantOverseer.ViewModels;
internal partial class MissingItemsViewModel : ViewModelBase
{
    private readonly SaveDataService _saveDataService;
    private MappedMissingItems _mappedMissingItems = new();
    private readonly Subject<string?> _filterTextSubject = new Subject<string?>();

    private int _selectedCharacterIndex = -1;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string? _filterText = null;

    [ObservableProperty]
    private ObservableCollection<ItemCategory> _filteredItemCategories = [];

    [ObservableProperty]
    private bool _isGlobalExpandOn = true;

    public MissingItemsViewModel(SaveDataService saveDataService)
    {
        _saveDataService = saveDataService;
        Task.Run(async () => { await ReadSave(true); this.IsActive = true; });
        _filterTextSubject
          .Throttle(TimeSpan.FromMilliseconds(400))
          .Subscribe(OnFilterTextChangedDebounced);
    }

    [RelayCommand]
    private void ExpandTreeNodes()
    {
        IsGlobalExpandOn = !IsGlobalExpandOn;
    }

    partial void OnFilterTextChanged(string? value)
    {
        _filterTextSubject.OnNext(value);
    }
    private void OnFilterTextChangedDebounced(string? value)
    {
        ApplyFilter(value);
    }

    [RelayCommand]
    public void ResetFilters()
    {
        FilterText = null;
    }

    private async Task ReadSave(bool resetActiveCahracter = false)
    {
        IsLoading = true;

        var dataset = await _saveDataService.GetSaveData();
        if (dataset == null)
        {
            IsLoading = false;
            return;
        }

        if (resetActiveCahracter)
        {
            _selectedCharacterIndex = dataset.ActiveCharacterIndex;
            // Call private field to avoid filtering on every assignment
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            _filterText = null;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            OnPropertyChanged(nameof(FilterText));
        }

        _mappedMissingItems = DatasetMapper.MapMissingItems(dataset.Characters[_selectedCharacterIndex].Profile.MissingItems);

        ApplyFilter();

        IsLoading = false;
    }

    private async Task CharacterUpdatedHandler(int characerIndex)
    {
        _selectedCharacterIndex = characerIndex;
        await ReadSave(false);
    }

    private void ApplyFilter()
    {
        ApplyFilter(FilterText);
    }

    private void ApplyFilter(string? value)
    {
        var tempFiltered = new List<ItemCategory>();
        foreach (var mappedCategory in _mappedMissingItems.ItemCategoryList)
        {
            if (mappedCategory.Type == Models.Enums.ItemTypes.Unknown) continue;

            var tempCategory = mappedCategory.ShallowCopy();
            tempCategory.Items = [];
            List<Item> tempItems = [];
            if (!string.IsNullOrEmpty(value))
            {
                tempItems = mappedCategory.Items.Where(i => i.Name.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                tempItems.AddRange(mappedCategory.Items);
            }
            if (tempItems.Count != 0) { tempCategory.Items = tempItems; tempFiltered.Add(tempCategory); }
        }

        FilteredItemCategories = new(tempFiltered);
    }

    #region Messages
    protected override void OnActivated()
    {
        Messenger.Register<MissingItemsViewModel, CharacterSelectChangedMessage>(this, (r, m) => {
            IsLoading = true; // Look into it later, sometimes task starts just a moment too late and the old stuff still can be seen
            Task.Run(async () => await CharacterUpdatedHandler(m.Value));
        });

        Messenger.Register<MissingItemsViewModel, SaveFileChangedMessage>(this, (r, m) => {
            IsLoading = true;
            Task.Run(async () => await ReadSave(m.CharacterCountChanged));
        });
    }
    #endregion Messages
}
