using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemnantOverseer.ViewModels;
public partial class CharacterSelectViewModel: ViewModelBase
{
    private readonly SaveDataService _saveDataService;

    private int _selectedCharacterIndex = -1;

    [ObservableProperty]
    private List<Character> _characters = [];

    [ObservableProperty]
    private bool _isLoading = false;

    public CharacterSelectViewModel(SaveDataService saveDataService)
    {
        _saveDataService = saveDataService;

        if(Design.IsDesignMode)
        {
            Characters =
            [
                new Character() { ObjectCount = 500, Archetype = Archetypes.Gunslinger, SubArchetype = Archetypes.Archon, Index = 0 },
                new Character() { ObjectCount = 123, Archetype = Archetypes.Medic, Index = 1 },
                new Character() { ObjectCount = 0, Archetype = Archetypes.Unknown, Index = 2 },
                new Character() { ObjectCount = 10, Archetype = Archetypes.Invader, Index = 3 },
            ];
            return;
        }

        Task.Run(async () => { await ReadSave(true); this.IsActive = true; });
    }

    [RelayCommand]
    public void CharacterSelected(Character selectedCharacter)
    {
        if (selectedCharacter.Index == _selectedCharacterIndex) return;

        foreach (var character in Characters)
        {
            character.IsSelected = character.Index == selectedCharacter.Index;
        }
        _selectedCharacterIndex = selectedCharacter.Index;
        Task.Run(async () =>
        {
            // Feeling TOO snappy without a delay
            await Task.Delay(125);
            Messenger.Send(new CharacterSelectChangedMessage(_selectedCharacterIndex));
        });
    }

    private async Task ReadSave(bool resetActiveCahracter)
    {
        IsLoading = true;

        var data = await _saveDataService.GetSaveData();
        if (data != null && data.Characters.Count > 0)
        {
            List<Character> mappedCharacters = [];
            try
            {
                mappedCharacters = DatasetMapper.MapCharacters(data.Characters).CharacterList;
            }
            catch(Exception ex)
            {
                IsLoading = false;
                // TODO: Handle this better when reworking error handling
                Messenger.Send(new NotificationErrorMessage("Could not load characters. Please report this error with attached zipped save folder" + Environment.NewLine + ex.Message));
            }
#if DEBUG
            //mappedCharacters.Add(new Character() { ObjectCount = 0, Archetype = Archetypes.Unknown, Index = 2 });
            //mappedCharacters.Add(new Character() { ObjectCount = 10, Archetype = Archetypes.Invader, Index = 3, PowerLevel = 4, Playtime = TimeSpan.FromHours(10) });
#endif
            if (resetActiveCahracter)
            {
                _selectedCharacterIndex = data.ActiveCharacterIndex;
            }

            if (_selectedCharacterIndex >= 0)
            {
                foreach (var character in mappedCharacters)
                {
                    character.IsSelected = character.Index == _selectedCharacterIndex;
                }
            }
            else
            {
                Messenger.Send(new NotificationWarningMessage(NotificationStrings.SelectedCharacterNotValid));
            }

            Characters = mappedCharacters;
        }

        IsLoading = false;
    }

    #region Messages
    protected override void OnActivated()
    {
        Messenger.Register<CharacterSelectViewModel, SaveFileChangedMessage>(this, (r, m) => {
            IsLoading = true;
            Task.Run(async () => await ReadSave(m.CharacterCountChanged));
        });
    }
    #endregion Messages
}
