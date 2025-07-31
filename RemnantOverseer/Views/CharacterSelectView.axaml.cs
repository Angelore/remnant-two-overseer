using Avalonia.Controls;
using Avalonia.Interactivity;
using RemnantOverseer.ViewModels;
using System;

namespace RemnantOverseer.Views;

public partial class CharacterSelectView : UserControl
{
    public CharacterSelectView()
    {
        if (Design.IsDesignMode)
        {
            // This can be before or after InitializeComponent.
            var settingsService = new Services.SettingsService();
            Design.SetDataContext(this, new CharacterSelectViewModel(new Services.SaveDataService(settingsService)));
        }
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext as CharacterSelectViewModel is null) throw new Exception("DataContext is still empty");
        ((CharacterSelectViewModel)DataContext).OnViewLoaded();
    }
}
