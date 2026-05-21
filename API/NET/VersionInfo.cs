using System.Text.Json.Serialization;

namespace SEUtilityTools.API.NET
{
    public class VersionInfo
    {
        public Version Version { get; set; } = new();

        [JsonIgnore]
        public string Tag => Version?.ToString() ?? string.Empty;
    }
}