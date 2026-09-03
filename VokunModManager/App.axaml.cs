using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using VokunModManager.Interfaces;
using VokunModManager.Misc;
using VokunModManager.ViewModels;
using VokunModManager.Views;

namespace VokunModManager;

public partial class App : Application
{
    // global service provider
    public IServiceProvider Services { get; private set; } = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // get ViewModel from container; all the dependencies will be included
            var vm = Services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
            
            await vm.UpdateAll();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // register singleton services
        services.AddSingleton<IAppConfig, AppConfig>(); // important, since I use AppConfig almost everywhere
        services.AddSingleton<IFileManager, FileManager>();
        services.AddSingleton<IAutoDetector, AutoDetector>();
        services.AddSingleton<ILoggerService, UiLoggerService>();
        services.AddSingleton<IModInstaller, FomodManager>(); // automatically applies ILoggerService to FomodManager ctor
        services.AddSingleton<IModListManager, ModListManager>();
        
        // register ViewModels
        services.AddTransient<MainWindowViewModel>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}