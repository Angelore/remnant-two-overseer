using Avalonia;
using Avalonia.Controls.Primitives;

namespace RemnantOverseer.Controls;

public class LocationCard : TemplatedControl
{

    /// <summary>
    /// Title StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<LocationCard, string>(nameof(Title), "Unknown");

    /// <summary>
    /// Gets or sets the Title property. This StyledProperty
    /// indicates the title :).
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
