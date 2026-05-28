using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RemnantOverseer.ViewModels;
using System;

namespace RemnantOverseer.Views;

public partial class MissingItemsView : UserControl
{
    public MissingItemsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext as MissingItemsViewModel is null) throw new Exception("DataContext is still empty");
        ((MissingItemsViewModel)DataContext).OnViewLoaded();

        // Focus the content so Ctrl+C works right after switching to this tab.
        ContentGrid.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        if (e.Key != Key.C && e.Key != Key.Insert) return;

        // When the search box is focused, let it copy its own text.
        if (FilterBox.IsKeyboardFocusWithin) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;

        if (DataContext is MissingItemsViewModel vm)
        {
            _ = vm.CopyToClipboardAsync(clipboard);
            e.Handled = true;
        }
    }
}