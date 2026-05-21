namespace SEUtilityTools.API.Data
{
    public class BlockData
    {
        public string Name {  get; set; } = string.Empty;
        public string DLCType { get; set; } = string.Empty;
        public string GridSize { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public int PCU { get; set; }
        public Dictionary<string, int> Material { get; set; }  = [];
    }
}