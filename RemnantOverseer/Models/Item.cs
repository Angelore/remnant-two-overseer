using RemnantOverseer.Models.Enums;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System;

namespace RemnantOverseer.Models;
public class Item
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public ItemTypes Type { get; set; }
    public WeaponSubtypes? WeaponSubtype { get; set; }
    public string Description { get; set; } = string.Empty;
    public OriginTypes OriginType { get; set; }
    public string OriginName { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public bool IsLooted { get; set; }
    public bool IsPrerequisiteMissing { get; set; }
    public bool HasRequiredMaterial { get; set; }
    public bool IsCoop { get; set; }
    public bool IsAccountAward { get; set; }

    public string TypeName => LocalizationService.ItemTypeName(Type);

    // We are only interested in a couple of types to display
    public string? OriginNameFormatted
    {
        get
        {
            return string.IsNullOrEmpty(OriginName) ? null :
            OriginType switch
            {
                // Extra spaces are a temporary workaround to https://github.com/AvaloniaUI/Avalonia/issues/17862, remove when fixed
                // It's not fixed yet, but I moved this text out of the tooltip. Removing spaces, keeping comment
                OriginTypes.Injectable or OriginTypes.Dungeon or OriginTypes.Vendor => LocalizationService.Format("Item_OriginNameFormat", LocalizationService.OriginTypeName(OriginType), OriginName),
                _ => null
            };
        }
    }

    private string LinkName => string.IsNullOrWhiteSpace(CanonicalName) ? Name : CanonicalName;

    public string? WikiLink => $"{UrlStrings.WikiUrl}/{LinkName}";

    public string? ToolkitLink => GetToolkitLink();

    public Item ShallowCopy()
    {
        return (Item)MemberwiseClone();
    }

    private string? GetToolkitLink()
    {
        var querySubstring = "";
        switch (Type)
        {
            case ItemTypes.Amulet:
                querySubstring = "amulets";
                break;
            case ItemTypes.Ring:
                querySubstring = "rings";
                break;
            case ItemTypes.Weapon:
                if (WeaponSubtype == null) return null;
                querySubstring = WeaponSubtype switch
                {
                    WeaponSubtypes.LongGun => "longGuns",
                    WeaponSubtypes.HandGun => "handGuns",
                    WeaponSubtypes.MeleeWeapon => "melees",
                    _ => throw new NotImplementedException()
                };
                break;
            default:
                return null;
        }
        var itemName = LinkName.Replace(" ", "+");
        return $"{UrlStrings.ToolkitUrl}?{querySubstring}={itemName}";
    }
}
