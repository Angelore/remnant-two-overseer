﻿using CommunityToolkit.Mvvm.ComponentModel;
using RemnantOverseer.Models.Enums;
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

    public string? Summary
    {
        get
        {
            return SubArchetype == null ? $"{Archetype} :: Level {FormattedPowerLevel}" : $"{Archetype}/{SubArchetype} :: Level {FormattedPowerLevel}";
        }
    }

    // Viewmodel specific
    [ObservableProperty]
    private bool _isSelected;
}
