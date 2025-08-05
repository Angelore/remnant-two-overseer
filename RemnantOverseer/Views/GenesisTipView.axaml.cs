using Avalonia.Controls;
using Avalonia.Interactivity;
using RemnantOverseer.ViewModels;
using System;

namespace RemnantOverseer.Views;

public partial class GenesisTipView : UserControl
{
    public GenesisTipView()
    {
        if (Design.IsDesignMode)
        {
            Design.SetDataContext(this, new GenesisTipViewModel());
        }
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
        base.OnUnloaded(e);
    }
}
