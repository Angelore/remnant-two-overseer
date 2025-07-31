using lib.remnant2.analyzer.Model;
using RemnantOverseer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RemnantOverseer.Utilities;
internal class DatasetMapper
{
    public static MappedZones MapCharacterToZones(Character characterData)
    {
        var missingItemIds = characterData.Profile.MissingItems.Select(x => x["Id"]).ToList();
        var result = new MappedZones
        {
            CampaignZoneList = MapZonesToZones([.. characterData.Save.Campaign.Zones, characterData.Save.Campaign.Ward13], missingItemIds, characterData.Save.Campaign.RespawnPoint)
        };
        if (characterData.Save.Adventure != null)
        {
            result.AdventureZoneList = MapZonesToZones([.. characterData.Save.Adventure.Zones, characterData.Save.Adventure.Ward13], missingItemIds, characterData.Save.Adventure.RespawnPoint);
        }
        return result;
    }

    public static MappedCharacters MapCharacters(List<Character> characters)
    {
        var result = new MappedCharacters();
        for (int index = 0; index < characters.Count; index++)
        {
            var character = characters[index];
            var mappedCharacter = MapCharacter(character, index);
            result.CharacterList.Add(mappedCharacter);
        }

        // Just in case
        result.CharacterList = [.. result.CharacterList.OrderBy((m) => m.Index)];

        return result;
    }

    public static Models.Character MapCharacter(Character character, int index)
    {
        Enum.TryParse<Archetypes>(character.Profile.Archetype, true, out var archetype); // If false, will default to default value in enum, i.e. Unknown
        Enum.TryParse<Archetypes>(character.Profile.SecondaryArchetype, true, out var subarchetype);
        return new Models.Character
        {
            Index = index,
            Archetype = archetype,
            SubArchetype = string.IsNullOrEmpty(character.Profile.SecondaryArchetype) ? null : subarchetype,
            ObjectCount = character.Profile.AcquiredItems,
            PowerLevel = character.Profile.ItemLevel, // Yes.
            ActiveWorld = Enum.Parse<WorldTypes>(character.ActiveWorldSlot.ToString(), true),
            IsHardcore = character.Profile.IsHardcore,
            Playtime = character.Save.Playtime ?? TimeSpan.Zero
        };
    }

    public static MappedMissingItems MapMissingItems(List<Dictionary<string, string>> missingItemsDict)
    {
        var result = new MappedMissingItems();
        foreach(var missingItem in missingItemsDict)
        {
            var lootItem = new LootItemExtended() { Properties = missingItem };
            var item = MapLootItemToItem(lootItem);
            result.ItemCategoryList[(int)item.Type].Items.Add(item);
        }

        return result;
    }

    public static int GetActiveCharacterIndex(Dataset dataset)
    {
        // Can have gaps due to removed/dead characters, e.g. 0, 1, 3, 4
        int[] indices = dataset.Characters.Select(c => c.Index).ToArray();
        var activeIndex = Array.IndexOf(indices, dataset.ActiveCharacterIndex);
        return activeIndex < 0 ? 0 : activeIndex;
    }

    public static Models.ThaenTree? MapThaenTree(Character character)
    {
        return character.Save.ThaenFruit is null ? null : new Models.ThaenTree()
        {
            GrowthStage = character.Save.ThaenFruit.GrowthStage,
            Timestamp = character.Save.ThaenFruit.Timestamp,
            HasFruit = character.Save.ThaenFruit.HasFruit,
            PickedCount = character.Save.ThaenFruit.PickedCount
        };
    }

    public static float? GetBloodmoonChance(RolledWorld? world)
    {
        if (world is null) return null;
        return world.BloodMoon?.CurrentChance is not null ? (float)Math.Round(world.BloodMoon.CurrentChance, 2, MidpointRounding.AwayFromZero) : null;
    }

    private static List<Models.Zone> MapZonesToZones(List<Zone> zones, List<string> missingItemIds, RespawnPoint? respawnPoint)
    {
        //var locnames = new List<string>();
        //var subtypes = new List<string>();

        var result = new List<Models.Zone>();
        foreach (var zone in zones)
        {
            var zoneModel = new Models.Zone
            {
                Name = zone.Name,
                Story = zone.Story,
                Locations = []
            };

            // Map Locations
            // TODO: Consider nesting locations
            foreach (var location in zone.Locations)
            {
                //locnames.Add(location.Name);
                var locationModel = new Models.Location
                {
                    Name = location.Name,
                    Items = [],
                    IsSimulacrumPresent = location.Simulacrum,
                    IsSimulacrumLooted = location.SimulacrumLooted,
                    IsTraitBookPresent = location.TraitBook,
                    IsTraitBookLooted = location.TraitBookLooted,

                    // not supported yet
                    IsBloodmoon = location.Bloodmoon
                };

                // Map Items
                foreach (var lootGroup in location.LootGroups)
                {
                    foreach (var item in lootGroup.Items)
                    {
                        //if (item.Properties.TryGetValue("Subtype", out string? value) && !subtypes.Contains(value))
                        //{
                        //    subtypes.Add(value);
                        //}
                        var itemModel = MapLootItemToItem(item, lootGroup, !missingItemIds.Contains(item.Id));
                        // if (itemModel.OriginName.Equals("Oracle's Refuge")) locationModel.IsOracleLocation = true;
                        locationModel.Items.Add(itemModel);
                    }
                }

                // Respawn point checks
                if (respawnPoint != null && respawnPoint.Type != lib.remnant2.analyzer.Enums.RespawnPointType.None)
                {
                    switch (respawnPoint.Type)
                    {
                        case lib.remnant2.analyzer.Enums.RespawnPointType.WorldStone:
                            if (location.WorldStones.Contains(respawnPoint.Name))
                            {
                                SetAsRespawnLocation(locationModel, respawnPoint);
                            }
                            break;
                        case lib.remnant2.analyzer.Enums.RespawnPointType.Checkpoint:
                            if (location.Name == respawnPoint.Name)
                            {
                                SetAsRespawnLocation(locationModel, respawnPoint);
                            }
                            break;
                        case lib.remnant2.analyzer.Enums.RespawnPointType.ZoneTransition:
                            var name = respawnPoint.Name.Split("/")[0];
                            if (location.Name == name)
                            {
                                SetAsRespawnLocation(locationModel, respawnPoint);
                            }
                            break;
                    }
                }

                zoneModel.Locations.Add(locationModel);
            }

            result.Add(zoneModel);
        }

        //var t = locnames;
        //var s = subtypes;
        return result;
    }

    private static Models.Item MapLootItemToItem(LootItemExtended lootItem)
    {
        Enum.TryParse<ItemTypes>(lootItem.Type.Replace("_", ""), true, out var itemType); // If false, will default to default value in enum, aka Unknown
        var itemModel = new Models.Item
        {
            Id = lootItem.Id,
            Name = lootItem.Name,
            Description = lootItem.ItemNotes,
            IsLooted = lootItem.IsLooted,
            IsPrerequisiteMissing = lootItem.IsPrerequisiteMissing,
            HasRequiredMaterial = lootItem.HasRequiredMaterial,
            Type = itemType,
            IsCoop = lootItem.Properties.ContainsKey("Coop") && lootItem.Properties["Coop"] == "True",
            IsAccountAward = lootItem.IsVendoredAccountAward
        };

        return itemModel;
    }

    private static Models.Item MapLootItemToItem(LootItemExtended lootItem, LootGroup lootGroup, bool isDuplicate)
    {
        var itemModel = MapLootItemToItem(lootItem);

        Enum.TryParse<OriginTypes>(lootGroup.Type.Replace(" ", ""), true, out var originType);
        itemModel.OriginType = originType;
        itemModel.OriginName = lootGroup.Name ?? string.Empty;
        itemModel.IsDuplicate = isDuplicate;

        if(itemModel.Type == ItemTypes.Weapon)
        {
            itemModel.WeaponSubtype = WeaponSubtypesExtensions.FromFriendlyString(lootItem.Properties["Subtype"]);
        }

        return itemModel;
    }

    private static void SetAsRespawnLocation(Models.Location locationModel, RespawnPoint respawnPoint)
    {
        locationModel.IsRespawnLocation = true;
        locationModel.RespawnPointName = respawnPoint.Name;
        locationModel.RespawnPointType = Enum.Parse<RespawnPointType>(respawnPoint.Type.ToString(), true);
    }
}

internal class MappedZones
{
    public List<Models.Zone> CampaignZoneList { get; set; } = [];
    public List<Models.Zone> AdventureZoneList { get; set; } = [];
}

internal class MappedCharacters
{
    public List<Models.Character> CharacterList { get; set; } = [];
}

internal class MappedMissingItems
{
    public List<Models.ItemCategory> ItemCategoryList { get; set; } = [];

    public MappedMissingItems()
    {
        foreach(var type in Enum.GetValues<ItemTypes>())
        {
            ItemCategoryList.Add(new Models.ItemCategory() { Type = type });
        }
    }
}