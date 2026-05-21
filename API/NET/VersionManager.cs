using System.Text.Json;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SEUtilityTools.API.Helpers;

namespace SEUtilityTools.API.NET
{
    public static class VersionManager
    {
        private const string API = "https://seversionmanager.thaumiel-servers.workers.dev";

        private static readonly HttpClient HttpClient = new();
        public static Dictionary<Version, VersionInfo> Versions { get; private set; } = [];

        public static async Task Init()
        {
            await GetVersions();
        }

        public static async Task GetVersions()
        {
            try
            {
                List<VersionInfo>? versionList = JsonSerializer.Deserialize<List<VersionInfo>>(await HttpClient.GetStringAsync($"{API}/versions"));
                if (versionList == null || versionList.Count == 0)
                {
                    LogManager.Warn("Version API returned empty or null response");
                    return;
                }

                Versions = versionList.Where(v => v.Version != null).ToDictionary(v => v.Version, v => v);
                await CheckCurrentVersion();
                LogManager.Info($"Successfully loaded {Versions.Count} version(s) from API");
            }
            catch (HttpRequestException ex)
            {
                LogManager.Error($"Network error while fetching versions: {ex.Message}");
                await ShowError($"Network error: {ex.Message}", "Connection Error");
            }
            catch (JsonException ex)
            {
                LogManager.Error($"Failed to parse version data: {ex.Message}");
                await ShowError($"Failed to parse version data: {ex.Message}", "Parse Error");
            }
            catch (Exception ex)
            {
                LogManager.Error($"Unexpected error while fetching versions: {ex}");
                await ShowError($"Unexpected error: {ex.Message}", "Error");
            }
        }

        private static async Task CheckCurrentVersion()
        {
            if (!Versions.TryGetValue(Program.Version, out VersionInfo? currentVersion))
            {
                LogManager.Info($"Current version {Program.Version} not found in version list");
                return;
            }
        }

        /// <summary>
        /// Safe message box wrapper. Falls back to Console if Avalonia isn't ready.
        /// </summary>
        private static async Task ShowMessageBox(string title, string message, ButtonEnum buttons, Icon icon)
        {
            try
            {
                await MessageBoxManager
                    .GetMessageBoxStandard(title, message, buttons, icon)
                    .ShowAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("IAssetLoader") || ex.Message.Contains("Unable to locate"))
            {
                Console.WriteLine($"[{icon}] {title}: {message}");
            }
        }

        private static async Task ShowError(string message, string title) =>
            await ShowMessageBox(title, message, ButtonEnum.Ok, Icon.Error);

        public static VersionInfo? GetVersionInfo(Version version) =>
            Versions.TryGetValue(version, out VersionInfo? info) ? info : null;

        public static VersionInfo? GetLatestVersion() =>
            Versions.Values.OrderByDescending(v => v.Version).FirstOrDefault();

        public static bool IsUpdateAvailable()
        {
            VersionInfo? latest = GetLatestVersion();
            return latest != null && latest.Version > Program.Version;
        }
    }
}