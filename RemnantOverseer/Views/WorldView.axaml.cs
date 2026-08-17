using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using RemnantOverseer.ViewModels;
using System;

namespace RemnantOverseer.Views;

public partial class WorldView : UserControl
{
    public WorldView()
    {
        if (Design.IsDesignMode)
        {
            // This can be before or after InitializeComponent.
            var settingsService = new Services.SettingsService();
            Design.SetDataContext(this, new WorldViewModel(settingsService, new Services.SaveDataService(settingsService)));
        }
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext as WorldViewModel is null) throw new Exception("DataContext is still empty");
        ((WorldViewModel)DataContext).OnViewLoaded();
    }

    // Build the bloodmoon tooltip on hover so it reflects the current time, and show the
    // extra debug lines when Ctrl+Shift is held while mousing over.
    private void BloodmoonIcon_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control || DataContext is not WorldViewModel vm) return;

        var debug = e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        // Supply our own ToolTip instance (Avalonia uses a Tip that is itself a ToolTip directly)
        // so we can lift the default 320px MaxWidth for this tooltip only - other tooltips in the
        // view still wrap as before. NoWrap then lets the box size to the widest debug line.
        ToolTip.SetTip(control, new ToolTip
        {
            MaxWidth = double.PositiveInfinity,
            Content = new TextBlock
            {
                Text = vm.GetBloodmoonTooltip(debug),
                TextWrapping = TextWrapping.NoWrap
            }
        });
    }

    // Flyout can only be shown by explicitly calling it
    private void FiltersButton_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Control control)
        {
            FlyoutBase.ShowAttachedFlyout(control);
        }
    }

    private void GenesisHintButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var flyout = new Flyout
        {
            Content = new GenesisTipViewModel(),
            Placement = PlacementMode.Center
        };
        flyout.ShowAt(ContentGrid);
    }

    private void ThaenHintButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var flyout = new Flyout
        {
            Content = new ThaenProgressViewModel(((WorldViewModel)DataContext!).ThaenTree),
            Placement = PlacementMode.Center
        };
        flyout.ShowAt(ContentGrid);
    }

    private void QuiltHintButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var flyout = new Flyout
        {
            Content = new PatchworkQuiltViewModel(((WorldViewModel)DataContext!).CompletedQuests),
            Placement = PlacementMode.Center
        };
        flyout.ShowAt(ContentGrid);
    }
}
