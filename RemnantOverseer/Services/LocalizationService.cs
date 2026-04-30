using RemnantOverseer.Models.Enums;
using RemnantOverseer.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace RemnantOverseer.Services;

public sealed record CultureOption(string CultureName, string DisplayName);

internal static class LocalizationService
{
    private static readonly ResourceManager ResourceManager = new(
        "RemnantOverseer.Resources.AppStrings",
        Assembly.GetExecutingAssembly());

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo(LocalizationConstants.DefaultCultureName);

    public static IReadOnlyList<CultureOption> SupportedCultures { get; } =
    [
        new CultureOption(LocalizationConstants.DefaultCultureName, Get("Language_English"))
    ];

    public static void ApplyCulture(string? cultureName)
    {
        var culture = GetCultureOrDefault(cultureName);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static string Get(string key)
    {
        var value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        if (value is not null)
        {
            return value;
        }

        return ResourceManager.GetString(key, EnglishCulture) ?? key;
    }

    public static string Format(string key, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }

    public static string ItemTypeName(ItemTypes type)
    {
        return Get($"ItemType_{type}");
    }

    public static string ItemTypePluralName(ItemTypes type)
    {
        return Get($"ItemType_{type}_Plural");
    }

    public static string WeaponSubtypeName(WeaponSubtypes subtype)
    {
        return Get($"WeaponSubtype_{subtype}");
    }

    public static string OriginTypeName(OriginTypes type)
    {
        return Get($"OriginType_{type}");
    }

    public static string ArchetypeName(Archetypes archetype)
    {
        return Get($"Archetype_{archetype}");
    }

    public static bool IsSupportedCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return false;
        }

        foreach (var culture in SupportedCultures)
        {
            if (string.Equals(culture.CultureName, cultureName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static CultureInfo GetCultureOrDefault(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return EnglishCulture;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            return culture.IsNeutralCulture ? CultureInfo.CreateSpecificCulture(culture.Name) : culture;
        }
        catch (CultureNotFoundException)
        {
            return EnglishCulture;
        }
    }
}
