using Avalonia;
using System;
using System.IO;

namespace ControllerBridge.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var crashLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
            var crashReport = $"[CRASH LOG - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]\n" +
                              $"Exception: {ex.GetType().FullName}\n" +
                              $"Message: {ex.Message}\n" +
                              $"Source: {ex.Source}\n\n" +
                              $"StackTrace:\n{ex.StackTrace}\n\n" +
                              $"InnerException:\n{ex.InnerException}\n";

            try
            {
                File.WriteAllText(crashLogPath, crashReport);
            }
            catch { }

            Console.Error.WriteLine(crashReport);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new Win32PlatformOptions
            {
                RenderingMode = new[]
                {
                    Win32RenderingMode.AngleEgl,
                    Win32RenderingMode.Wgl,
                    Win32RenderingMode.Software
                },
                CompositionMode = new[]
                {
                    Win32CompositionMode.WinUIComposition,
                    Win32CompositionMode.LowLatencyDxgiSwapChain,
                    Win32CompositionMode.RedirectionSurface
                }
            })
            .LogToTrace();
}
