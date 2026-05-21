using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.Yaml;
using System.Net;
using System.Runtime.InteropServices;
using ImageConverter = SEUtilityTools.API.Helpers.ImageConverter;

namespace SEUtilityTools;

static class Program
{
#if WINDOWS
    [DllImport("kernel32.dll")] private static extern bool AllocConsole();
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(int dwProcessId);
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    private const int ATTACH_PARENT_PROCESS = -1;
#endif

    public static Version Version = new(1, 0, 0);
    public static event Action? ApplicationStart;
    public static Config Config => ConfigManager.Config!;

    [STAThread]
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, ev) =>
        {
            if (ev.ExceptionObject is Exception ex)
                HandleCrashAsync(ex).GetAwaiter().GetResult();
        };

        TaskScheduler.UnobservedTaskException += (_, ev) =>
        {
            HandleCrashAsync(ev.Exception).GetAwaiter().GetResult();
            ev.SetObserved();
        };

        if (args.Contains("--console") || Config.Debug)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#if WINDOWS
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                    AllocConsole();
#endif
            }

            LogManager.Info("Console started, showing logs...");
        }

        YamlConfig.Init();

        if (Config.ConvertIconsOnStart)
        {
            await ImageConverter.ConvertDirectoryAsync(Path.Combine(Config.SpaceEngineersDirectory, "Content", "Textures", "GUI", "Icons", "Cubes"));
            await ImageConverter.ConvertDirectoryAsync(Path.Combine(Config.SpaceEngineersDirectory, "Content", "Textures", "GUI", "Icons", "component"), outputDir: "ConvertedComponents");
        }

        AppBuilder appBuilder = BuildAvaloniaApp();
        appBuilder.StartWithClassicDesktopLifetime(args);

        ApplicationStart?.Invoke();
        LogManager.SaveLogs();
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static async Task HandleCrashAsync(Exception ex)
    {
        try
        {
            LogManager.Critical($"Application crashed: {ex}");
            LogManager.SaveLogs();

            var result = await ShowCrashDialogAsync(ex);

            if (result)
            {
                (HttpStatusCode status, HttpContent _, string? size) = await LogManager.SendReportAsync();
                await ShowMessageAsync($"Log report upload {(status == HttpStatusCode.OK ? "succeeded" : "failed")} ({size})", "Upload Result");
            }
            else
            {
                await ShowMessageAsync("Logs were saved locally and were not uploaded.", "Logs Saved");
            }
        }
        catch { /* ignore crash-handler failures */ }
        finally
        {
            Environment.Exit(1);
        }
    }

    private static async Task<bool> ShowCrashDialogAsync(Exception ex)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } win })
        {
            return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(
                    "Crash Detected",
                    $"SEUtilityTools has encountered a crash.\n\n{ex.Message}\n\nDo you want to upload the logs to help improve the tool?",
                    ButtonEnum.YesNo,
                    Icon.Error
                );

                ButtonResult result = await box.ShowWindowDialogAsync(win);
                return result == ButtonResult.Yes;
            });
        }

        Console.Error.WriteLine($"Crash: {ex.Message}\nUpload logs? (y/n)");
        return Console.ReadLine()?.Trim().ToLower() == "y";
    }

    public static async Task ShowMessageAsync(string message, string title)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } win })
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(
                    title,
                    message,
                    ButtonEnum.Ok,
                    Icon.Info
                );

                await box.ShowWindowDialogAsync(win);
            });

            return;
        }

        Console.WriteLine($"[{title}] {message}");
    }
}