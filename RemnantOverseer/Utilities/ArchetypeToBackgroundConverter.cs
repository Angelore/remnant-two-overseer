using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using RemnantOverseer.Models.Enums;
using System;
using System.Globalization;

namespace RemnantOverseer.Utilities;
public class ArchetypeToBackgroundConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || value.GetType() != typeof(Archetypes))
        {
            return AvaloniaProperty.UnsetValue;
        }
        else
        {
            // What?
            // https://stackoverflow.com/a/13532993/8362950
            var color = Color.Parse(ArchetypeColors.Map[(Archetypes)value]);
            if (parameter is string percent && !string.IsNullOrEmpty(percent))
            {
                var percentNum = int.Parse(percent);
                if (percentNum > 100 || percentNum < -100)
                    return AvaloniaProperty.UnsetValue;

                var r = color.R * (100 + percentNum) / 100;
                var g = color.G * (100 + percentNum) / 100;
                var b = color.B * (100 + percentNum) / 100;

                r = r < 255 ? r : 255;
                g = g < 255 ? g : 255;
                b = b < 255 ? b : 255;

                color = new Color(color.A, (byte)r, (byte)g, (byte)b);
            }
            return new SolidColorBrush(color);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
