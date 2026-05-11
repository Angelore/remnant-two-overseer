using CommunityToolkit.Mvvm.ComponentModel;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RemnantOverseer.Models;
public class Location : ObservableObject
{
    public string CanonicalName { get; set; } = string.Empty;
    public string Name
    {
        get => LocalizationService.GameString(CanonicalName, CanonicalName);
        set => CanonicalName = value.Trim();
    }
    public List<Item> Items { get; set; } = [];
    public bool IsTraitBookPresent { get; set; }
    public bool IsSimulacrumPresent { get; set; }
    public bool IsTraitBookLooted { get; set; }
    public bool IsSimulacrumLooted { get; set; }
    public bool IsBloodmoon { get; set; }
    public List<string> CanonicalConnections { get; set; } = [];
    public bool HasConnections => CanonicalConnections.Count != 0;
    public string FormattedConnections => LocalizationService.Format("Location_Connections", string.Join(", ", CanonicalConnections.Select(LocalizeConnectionName)));

    public bool IsRespawnLocation { get; set; }
    public RespawnPointType RespawnPointType { get; set; }
    public string RespawnPointName { get; set; } = string.Empty;
    public string? FormattedRespawnPointName
    {
        get
        {
            if (!IsRespawnLocation) return null;
            return RespawnPointType switch
            {
                RespawnPointType.WorldStone => LocalizationService.Format("Location_WorldStoneRespawn", LocalizeLocationName(RespawnPointName)),
                RespawnPointType.Checkpoint => LocalizationService.Format("Location_CheckpointRespawn", LocalizeLocationName(RespawnPointName)),
                RespawnPointType.ZoneTransition => GetFormattedZoneTransition(),
                _ => null
            };
        }
    }

    private string GetFormattedZoneTransition()
    {
        var split = RespawnPointName.Split('/');
        return LocalizationService.Format("Location_ZoneTransitionRespawn", LocalizeLocationName(split[0]), LocalizeLocationName(split[1]));
    }

    private static string LocalizeLocationName(string value)
    {
        var canonical = value.Trim();
        return LocalizationService.GameString(canonical, canonical);
    }

    private static string LocalizeConnectionName(string value)
    {
        var canonical = value.Trim();
        var duplicateSuffixIndex = canonical.LastIndexOf(" x", StringComparison.Ordinal);

        if (duplicateSuffixIndex > 0
            && duplicateSuffixIndex + 2 < canonical.Length
            && int.TryParse(canonical[(duplicateSuffixIndex + 2)..], out _))
        {
            return $"{LocalizeLocationName(canonical[..duplicateSuffixIndex])}{canonical[duplicateSuffixIndex..]}";
        }

        return LocalizeLocationName(canonical);
    }

    public bool IsGenesisLocation => CanonicalName.Equals("Withered Necropolis");
    public bool IsWard13Location => CanonicalName.Equals("Ward 13");

    // Trying this out. Should not be a big performance hit since it's just ~10 calls
    private string[] _possibleOracleSpawns = ["Morrow Parish", "Forsaken Quarter", "Ironborough", "Brocwithe Quarter"];
    public bool IsOracleLocation => _possibleOracleSpawns.Contains(CanonicalName) && Items.Any(i => i.CanonicalOriginName.Equals("Oracle's Refuge", System.StringComparison.Ordinal));


    public Location ShallowCopy()
    {
        return (Location)MemberwiseClone();
    }

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(FormattedRespawnPointName));
        OnPropertyChanged(nameof(FormattedConnections));
    }
}
