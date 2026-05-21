using System.Diagnostics;
using System.Runtime.InteropServices;
using SEUtilityTools.API.Helpers;

namespace SEUtilityTools.API.NET
{
    public static class Updater
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task ApplyUpdateAsync(VersionInfo latest, IProgress<double>? progress = null)
        {
            string currentExe = GetCurrentExePath();
            string assetName = GetAssetName(currentExe);
            string url = $"https://github.com/Thaumiel-Team/SEUtilityTools/releases/download/{latest.Tag}/{assetName}";

            LogManager.Info($"Downloading update from {url}");

            string tempFile = Path.Combine(Path.GetTempPath(), assetName);
            await DownloadWithProgressAsync(url, tempFile, progress);

            LogManager.Info($"Download complete. Applying update...");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ApplyWindows(currentExe, tempFile);
            }
            else
                await ApplyUnixAsync(currentExe, tempFile);
        }

        private static void ApplyWindows(string currentExe, string tempFile)
        {
            string script = Path.Combine(Path.GetTempPath(), "seut_update.bat");
            string pid = Environment.ProcessId.ToString();

            File.WriteAllText(script, $"""
                @echo off
                :waitloop
                tasklist /fi "PID eq {pid}" 2>nul | find "{pid}" >nul
                if not errorlevel 1 (
                    timeout /t 1 /nobreak >nul
                    goto waitloop
                )
                copy /y "{tempFile}" "{currentExe}"
                start "" "{currentExe}"
                del "{tempFile}"
                del "%~f0"
                """);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Environment.Exit(0);
        }

        private static async Task ApplyUnixAsync(string currentExe, string tempFile)
        {
            File.Copy(tempFile, currentExe, overwrite: true);
            File.Delete(tempFile);

            await Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{currentExe}\"",
                UseShellExecute = false
            })!.WaitForExitAsync();

            Process.Start(new ProcessStartInfo
            {
                FileName = currentExe,
                UseShellExecute = true
            });

            Environment.Exit(0);
        }

        private static string GetAssetName(string currentExe)
        {
            bool sc = Path.GetFileNameWithoutExtension(currentExe).Contains("SC", StringComparison.OrdinalIgnoreCase);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return sc ? "SEUtilityTools-Windows-SC.exe" : "SEUtilityTools-Windows.exe";

            return sc ? "SEUtilityTools-Linux-SC" : "SEUtilityTools-Linux";
        }

        private static string GetCurrentExePath()
            => Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Cannot determine current executable path.");

        private static async Task DownloadWithProgressAsync(string url, string dest, IProgress<double>? progress)
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;

            await using Stream src = await response.Content.ReadAsStreamAsync();
            await using Stream file = File.Create(dest);

            byte[] buffer = new byte[81920];
            long read = 0;
            int bytes;

            while ((bytes = await src.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, bytes));
                read += bytes;

                if (total.HasValue)
                    progress?.Report((double)read / total.Value);
            }
        }
    }
}