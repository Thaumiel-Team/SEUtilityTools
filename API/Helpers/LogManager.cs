using System.Diagnostics;
using System.Net;
using System.Text;
using SEUtilityTools.API.NET;

namespace SEUtilityTools.API.Helpers
{
    public class LogManager
    {
        public class Log(string msg, LogSeverity level, DateTime timeStamp)
        {
#pragma warning disable CS9124 // Parameter is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
            public string Message { get; set; } = msg;
            public LogSeverity Level { get; set; } = level;
            public DateTime TimeStamp { get; set; } = timeStamp;
#pragma warning restore CS9124 // Parameter is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.

            public override string ToString() => $"[{timeStamp.Year}-{timeStamp.Month}-{timeStamp.Day} {timeStamp.Hour}:{timeStamp.Minute}:{timeStamp.Second}]  {Level}  [MERToolbox] {msg}";
        }

        public enum LogSeverity
        {
            Info,
            Warning,
            Error,
            Debug,
            Critical,
            Silent
        }

        public static List<Log> Logs = [];
        private static readonly string LogDirectory = "logs";
        public static bool MessageSent;

        public static void Info(string message)
        {
            Logs.Add(new(message, LogSeverity.Info, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [INFO] {message}");
            Console.ResetColor();
        }

        public static void Warn(string message)
        {
            Logs.Add(new(message, LogSeverity.Warning, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [WARN] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Logs.Add(new(message, LogSeverity.Error, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [ERROR] {message}");
            Console.ResetColor();
        }

        public static void Debug(string message)
        {
            Logs.Add(new(message, LogSeverity.Debug, DateTime.Now));
            if (Program.Config.Debug || VersionManager.ForceDebug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [DEBUG] {message}");
                Console.ResetColor();
            }
        }

        public static void Critical(string message)
        {
            Logs.Add(new(message, LogSeverity.Critical, DateTime.Now));
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [CRITICAL] {message}");
            Console.ResetColor();
        }

        public static void Silent(string message) =>
            Logs.Add(new(message, LogSeverity.Silent, DateTime.Now));

        public static string RetriveString(HttpContent response)
        {
            if (response is null)
                return string.Empty;

            Task<string> String = Task.Run(response.ReadAsStringAsync);
            String.Wait();

            return String.Result;
        }

        internal static async Task<(HttpStatusCode statusCode, HttpContent content)> ShareLogsAsync(string data, Exception? ex = null)
        {
            try
            {
                Dictionary<string, string> crashData = new()
                {
                    ["version"] = Program.Version.ToString(3),
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    
                    ["exception_type"] = ex?.GetType().FullName ?? "Unknown",
                    ["exception_message"] = ex?.Message ?? "No exception provided",
                    ["stack_trace"] = ex?.StackTrace ?? "",
                    ["inner_exception"] = ex?.InnerException?.ToString() ?? "",
                    
                    ["context"] = data,
                    
                    ["os_version"] = Environment.OSVersion.ToString(),
                    ["clr_version"] = Environment.Version.ToString(),
                    ["is_64bit"] = Environment.Is64BitOperatingSystem.ToString(),
                    
                    ["uptime_seconds"] = ((int)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds).ToString(),
                    ["memory_usage_mb"] = (Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024).ToString(),
                    ["thread_count"] = Process.GetCurrentProcess().Threads.Count.ToString(),
                };

                string queryString = string.Join("&", crashData.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                string url = $"https://selogs.thaumiel-servers.workers.dev/error?{queryString}";

                using StringContent content = new(data, Encoding.UTF8, "text/plain");
                HttpClient client = new();
                using HttpResponseMessage response = await client.PutAsync(url, content).ConfigureAwait(false);

                HttpContent responseContent = null!;
                if (response.Content != null)
                {
                    string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    responseContent = new StringContent(responseString, Encoding.UTF8, "application/json");
                }

                return (response.StatusCode, responseContent);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
        }

        public static async Task<(HttpStatusCode statusCode, HttpContent content, string readableSize)> SendReportAsync()
        {
            if (Logs.Count == 0)
            {
                Warn("You have no logs!");
                return (HttpStatusCode.Forbidden, null, null)!;
            }

            string formattedContent = await Task.Run(() => FormatLogsForReport()).ConfigureAwait(false);

            int byteSize = Encoding.UTF8.GetByteCount(formattedContent);
            string readableSize = byteSize switch
            {
                < 1024 => $"{byteSize} bytes",
                < 1024 * 1024 => $"{(byteSize / 1024.0):F2} KB",
                _ => $"{(byteSize / 1024.0 / 1024.0):F2} MB"
            };

            try
            {
                var (statusCode, content) = await ShareLogsAsync(formattedContent).ConfigureAwait(false);

                if (statusCode == HttpStatusCode.OK)
                    MessageSent = true;

                return (statusCode, content, readableSize);
            }
            catch (HttpRequestException ex)
            {
                Error($"HTTP error during log upload: {ex.Message}");
                return (HttpStatusCode.ServiceUnavailable, null, readableSize)!;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Error("Log upload timed out");
                return (HttpStatusCode.RequestTimeout, null, readableSize)!;
            }
            catch (Exception ex)
            {
                Error($"Unexpected error during log upload: {ex.Message}");
                return (HttpStatusCode.InternalServerError, null, readableSize)!;
            }
        }

        private static string FormatLogsForReport()
        {
            StringBuilder sb = new();

            sb.AppendLine("╔═══════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                 Space Engineers Utility Tools                     ║");
            sb.AppendLine("║                           Log Report                              ║");
            sb.AppendLine("╚═══════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            sb.AppendLine($"Total Entries: {Logs.Count}");

            var logSummary = Logs.GroupBy(h => h.Level).ToDictionary(g => g.Key, g => g.Count());

            sb.AppendLine("Log Level Summary:");
            foreach (var kvp in logSummary.OrderByDescending(x => x.Value))
                sb.AppendLine($"  • {kvp.Key}: {kvp.Value} entries");

            sb.AppendLine();

            if (Logs.Count > 0)
            {
                DateTimeOffset firstLog = Logs.First().TimeStamp;
                DateTimeOffset lastLog = Logs.Last().TimeStamp;
                sb.AppendLine($"Time Range: {firstLog:yyyy-MM-dd HH:mm:ss} to {lastLog:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Duration: {lastLog - firstLog}");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("                              LOG ENTRIES");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            var groupedLogs = Logs.GroupBy(h => h.Level).OrderBy(g => GetLogLevelPriority(g.Key));

            foreach (var group in groupedLogs)
            {
                sb.AppendLine($"┌─ {group.Key} LOGS ({group.Count()} entries) ─");
                sb.AppendLine();

                foreach (Log entry in group)
                    sb.AppendLine(FormatLogEntry(entry));

                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("                            RAW LOGS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            foreach (Log element in Logs)
                sb.AppendLine($"{element}");

            sb.AppendLine();
            return sb.ToString();
        }

        private static int GetLogLevelPriority(LogSeverity level)
        {
            return level switch
            {
                LogSeverity.Critical => 0,
                LogSeverity.Error => 1,
                LogSeverity.Warning => 2,
                LogSeverity.Info => 3,
                LogSeverity.Debug => 4,
                LogSeverity.Silent => 5,
                _ => 8
            };
        }

        private static string FormatLogEntry(Log entry)
        {
            string timestamp = entry.TimeStamp.ToString("HH:mm:ss.fff");
            string errorPart = entry.Level.ToString();

            return $" {timestamp} │ {errorPart}{entry.Message}";
        }

        public static void SaveLogs()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, LogDirectory)))
                    Directory.CreateDirectory(LogDirectory);

                string filename = $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                string filepath = Path.Combine(AppContext.BaseDirectory, LogDirectory, filename);

                using StreamWriter writer = new(filepath);
                writer.WriteLine($"=== Log Session - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                writer.WriteLine();

                foreach (Log log in Logs)
                    writer.WriteLine($"[{log.TimeStamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Logs saved to {filepath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to save logs: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}