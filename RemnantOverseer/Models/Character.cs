using CommunityToolkit.Mvvm.ComponentModel;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Services;
using System;

namespace RemnantOverseer.Models;
public partial class Character: ObservableObject
{
    public int Index { get; set; }
    public Archetypes Archetype { get; set; }
    public Archetypes? SubArchetype { get; set; }
    public int ObjectCount { get; set; }
    public bool IsHardcore { get; set; }
    public int PowerLevel { get; set; }
    public TimeSpan Playtime { get; set; }
    public WorldTypes ActiveWorld { get; set; }

    public string? FormattedPlaytime => (int)Playtime.TotalHours + Playtime.ToString(@"\:mm\:ss");

    public string FormattedPowerLevel => PowerLevel > 0 ? PowerLevel.ToString() : "?";

    public string ArchetypeName => LocalizationService.ArchetypeName(Archetype);

    public string? SubArchetypeName => SubArchetype is null ? null : LocalizationService.ArchetypeName(SubArchetype.Value);

    public string? Summary
    {
        get
        {
            return SubArchetypeName is null
                ? LocalizationService.Format("Character_SummarySingle", ArchetypeName, FormattedPowerLevel)
                : LocalizationService.Format("Character_SummaryDual", ArchetypeName, SubArchetypeName, FormattedPowerLevel);
        }
    }

    // Viewmodel specific
    [ObservableProperty]
    private bool _isSelected;

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(ArchetypeName));
        OnPropertyChanged(nameof(SubArchetypeName));
        OnPropertyChanged(nameof(Summary));
    }
}
