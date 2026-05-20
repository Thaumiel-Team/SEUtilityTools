namespace SEUtilityTools
{
    public class Config
    {
        public bool Debug { get; set; }
        public bool ConvertIconsOnStart { get; set; } = true;

#if WINDOWS
        public string SpaceEngineersDirectory { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers";
#else
        public string SpaceEngineersDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam", "steamapps", "common", "SpaceEngineers");
#endif
    }
}