namespace SEUtilityTools.API.NET
{
    public class VersionInfo
    {
        public string CodeName { get; set; } = string.Empty;
        public Version Version { get; set; } = new();
        public bool Recalled { get; set; }
        public string RecallReason { get; set; } = string.Empty;
        public bool PreRelease { get; set; }
        public bool ForceDebug { get; set; }
    }
}