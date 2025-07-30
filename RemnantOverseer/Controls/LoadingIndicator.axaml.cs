using Avalonia;
using Avalonia.Controls.Primitives;

namespace RemnantOverseer.Controls;

public class LoadingIndicator : TemplatedControl
{

    /// <summary>
    /// IsLoading StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<LoadingIndicator, bool>(nameof(IsLoading), false);

    /// <summary>
    /// Gets or sets the IsLoading property. This StyledProperty
    /// indicates that loading is taking place.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}
