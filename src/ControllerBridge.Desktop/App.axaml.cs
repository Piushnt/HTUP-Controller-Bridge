using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ControllerBridge.Acquisition;
using ControllerBridge.Desktop.Services;
using ControllerBridge.Desktop.ViewModels;
using ControllerBridge.Desktop.Views;
using ControllerBridge.Transport.WiFi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace ControllerBridge.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Core & Acquisition & Transport
        services.AddSingleton<ControllerBackendManager>();
        services.AddSingleton(sp => new UdpControllerServer(sp.GetRequiredService<ILogger<UdpControllerServer>>(), 5555));
        services.AddSingleton<BridgeEngine>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        // Start bridge engine
        var engine = Services.GetRequiredService<BridgeEngine>();
        await engine.StartAsync();

        if (ApplicationLifetime is IClassicDesktopApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };

            desktop.Exit += async (s, e) =>
            {
                await engine.StopAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
