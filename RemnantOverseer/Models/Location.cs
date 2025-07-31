using RemnantOverseer.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace RemnantOverseer.Models;
public class Location
{
    public string Name { get; set; } = string.Empty;
    public List<Item> Items { get; set; } = [];
    public bool IsTraitBookPresent { get; set; }
    public bool IsSimulacrumPresent { get; set; }
    public bool IsTraitBookLooted { get; set; }
    public bool IsSimulacrumLooted { get; set; }
    public bool IsBloodmoon { get; set; }

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
                RespawnPointType.WorldStone => $"World Stone: {RespawnPointName}",
                RespawnPointType.Checkpoint => $"Checkpoint: {RespawnPointName}",
                RespawnPointType.ZoneTransition => GetFormattedZoneTransition(),
                _ => null
            };
        }
    }

    private string GetFormattedZoneTransition()
    {
        var split = RespawnPointName.Split('/');
        return $"Transition between {split[0]} and {split[1]}";
    }

    public bool IsGenesisLocation => Name.Equals("Withered Necropolis");
    public bool IsWard13Location => Name.Equals("Ward 13");

    // Trying this out. Should not be a big performance hit since it's just ~10 calls
    private string[] _possibleOracleSpawns = ["Morrow Parish", "Forsaken Quarter", "Ironborough", "Brocwithe Quarter"];
    public bool IsOracleLocation => _possibleOracleSpawns.Contains(Name) && Items.Any(i => i.OriginName.Equals("Oracle's Refuge", System.StringComparison.Ordinal));


    public Location ShallowCopy()
    {
        return (Location)MemberwiseClone();
    }
}
