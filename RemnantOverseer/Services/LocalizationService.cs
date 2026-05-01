using RemnantOverseer.Models.Enums;
using RemnantOverseer.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace RemnantOverseer.Services;

public sealed record CultureOption(string CultureName, string DisplayName);

internal static class LocalizationService
{
    private static readonly Assembly ResourcesAssembly = Assembly.GetExecutingAssembly();

    private static readonly ResourceManager ResourceManager = new(
        $"{ResourcesAssembly.GetName().Name}.Resources.AppStrings",
        ResourcesAssembly);

    private static readonly ResourceManager GameResourceManager = new(
        $"{ResourcesAssembly.GetName().Name}.Resources.GameStrings",
        ResourcesAssembly);

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo(LocalizationConstants.DefaultCultureName);

    public static LocalizationBindingSource BindingSource { get; } = new();

    public static IReadOnlyList<CultureOption> SupportedCultures =>
    [
        new CultureOption(LocalizationConstants.DefaultCultureName, "English"),
        new CultureOption("de", "Deutsch"),
        new CultureOption("es", "Español"),
        new CultureOption("fr", "Français"),
        new CultureOption("it", "Italiano"),
        new CultureOption("ja", "日本語"),
        new CultureOption("ko", "한국어"),
        new CultureOption("pt-BR", "Português (Brasil)"),
        new CultureOption("ru", "Русский"),
        new CultureOption("zh-Hans", "简体中文")
    ];

    public static void ApplyCulture(string? cultureName)
    {
        var culture = GetCultureOrDefault(cultureName);
        var isCultureChanged = !string.Equals(CultureInfo.CurrentUICulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        if (isCultureChanged)
        {
            BindingSource.NotifyCultureChanged();
        }
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

    public static string GameString(string key, string fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return fallback;
        }

        return GetResourceString(GameResourceManager, key, CultureInfo.CurrentUICulture)
            ?? GetResourceString(GameResourceManager, key, EnglishCulture)
            ?? GetResourceString(ResourceManager, key, CultureInfo.CurrentUICulture)
            ?? GetResourceString(ResourceManager, key, EnglishCulture)
            ?? fallback;
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

    private static string? GetResourceString(ResourceManager resourceManager, string key, CultureInfo culture)
    {
        var value = resourceManager.GetString(key, culture);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

public sealed class LocalizationBindingSource : INotifyPropertyChanged
{
    private int _cultureVersion;

    public int CultureVersion
    {
        get => _cultureVersion;
        private set
        {
            if (_cultureVersion == value)
            {
                return;
            }

            _cultureVersion = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CultureVersion)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyCultureChanged()
    {
        CultureVersion++;
    }
}
