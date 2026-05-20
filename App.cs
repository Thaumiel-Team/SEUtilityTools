using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Projektanker.Icons.Avalonia;           // Fixed: was Optris.Icons.Avalonia
using Projektanker.Icons.Avalonia.FontAwesome; // Fixed: was Optris.Icons.Avalonia.FontAwesome
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.NET;

namespace SEUtilityTools
{
    public partial class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            IconProvider.Current.Register<FontAwesomeIconProvider>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
            _ = InitializeAsync();
        }

        private static async Task InitializeAsync()
        {
            try
            {
                ConfigManager.Init();
                await BlockManager.Init();
                await VersionManager.Init();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Initialization failed: {ex.Message}");
            }
        }
    }
}