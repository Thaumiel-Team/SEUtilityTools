using Avalonia.Media.Imaging;

namespace SEUtilityTools.API.Data
{
    public class BlueprintData
    {
        public required Bitmap Icon { get; set; }
        public string Folder { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<BlockData> Blocks { get; set; } = [];
    }
}