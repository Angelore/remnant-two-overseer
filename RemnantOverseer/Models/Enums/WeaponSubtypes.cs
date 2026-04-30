using RemnantOverseer.Services;
using System;

namespace RemnantOverseer.Models.Enums;
public enum WeaponSubtypes
{
    LongGun,
    HandGun,
    MeleeWeapon
}

public static class WeaponSubtypesExtensions
{
    public static WeaponSubtypes? FromFriendlyString(this string subtype)
    {
        return subtype switch
        {
            "Long Gun" => WeaponSubtypes.LongGun,
            "Hand Gun" => WeaponSubtypes.HandGun,
            "Melee Weapon" => WeaponSubtypes.MeleeWeapon,
            _ => null
        };
    }

    public static string ToFriendlyString(this WeaponSubtypes subtype)
    {
        return subtype switch
        {
            WeaponSubtypes.LongGun => LocalizationService.WeaponSubtypeName(WeaponSubtypes.LongGun),
            WeaponSubtypes.HandGun => LocalizationService.WeaponSubtypeName(WeaponSubtypes.HandGun),
            WeaponSubtypes.MeleeWeapon => LocalizationService.WeaponSubtypeName(WeaponSubtypes.MeleeWeapon),
            _ => throw new NotImplementedException()
        };
    }
}
