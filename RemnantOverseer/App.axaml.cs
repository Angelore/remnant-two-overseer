using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemnantOverseer.Services;
using RemnantOverseer.ViewModels;
using RemnantOverseer.Views;
using System;

namespace RemnantOverseer;
public partial class App : Application
{
    #region Dependency Injection
    public IHost AppHost
    {
        get;
    }

    public static T Resolve<T>()
        where T : class
    {
        if ((Current as App)!.AppHost.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.axaml.cs.");
        }

        return service;
    }
    #endregion Dependency Injection

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                services
                    .AddSingleton<SettingsService>()
                    .AddSingleton<SaveDataService>()
                    .AddTransient<MainWindowViewModel>()
                    .AddSingleton<CharacterSelectViewModel>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<MissingItemsViewModel>()
                    .AddSingleton<WorldViewModel>();
                    //.AddTransient<DebugViewModel>();
            })
            .Build();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Log.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = App.Resolve<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
