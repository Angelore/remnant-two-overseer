using Avalonia.Markup.Xaml;
using System;

namespace RemnantOverseer.Services;

public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizationService.Get(Key);
    }
}
