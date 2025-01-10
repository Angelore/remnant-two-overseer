using Avalonia.Controls;
using RemnantOverseer.ViewModels;

namespace RemnantOverseer.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        if (Design.IsDesignMode)
        {
            // This can be before or after InitializeComponent.
            var settingsService = new Services.SettingsService();
            Design.SetDataContext(this, new SettingsViewModel(settingsService));
        }
        InitializeComponent();
    }

    private void AmountOfRollingBackupsSpinner_Spin(object? sender, Avalonia.Controls.SpinEventArgs e)
    {
        if (DataContext == null) return;
        ((SettingsViewModel)DataContext).AmountOfRollingBackupsSpinnerSpinCommand.Execute(e.Direction.ToString());
    }

    private void MinutesBetweenRollingBackupsSpinner_Spin(object? sender, Avalonia.Controls.SpinEventArgs e)
    {
        if (DataContext == null) return;
        ((SettingsViewModel)DataContext).MinutesBetweenRollingBackupsSpinnerSpinCommand.Execute(e.Direction.ToString());
    }
}