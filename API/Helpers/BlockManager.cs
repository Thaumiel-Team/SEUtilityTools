using SEUtilityTools.API.Data;
using SEUtilityTools.API.Yaml;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SEUtilityTools.API.Helpers
{
    public static class BlockManager
    {
        private static string _path = Path.Combine(Program.Config.SpaceEngineersDirectory, "Content", "Data", "CubeBlocks");
        public static Dictionary<string, BlockData> Blocks = [];

        public static async Task Init()
        {
            Blocks = await GetData();
        }

        private static string ParseSubtypeName(string subtypeId)
        {
            if (string.IsNullOrEmpty(subtypeId))
                return subtypeId;

            return Regex.Replace(subtypeId, "(?<!^)([A-Z])", " $1");
        }

        private static async Task<Dictionary<string, BlockData>> GetData()
        {
            Dictionary<string, BlockData> data = [];

            string iconsDir = Path.Combine(AppContext.BaseDirectory, "ConvertedIcons");
            Dictionary<string, string> iconLookup = [];
            if (Directory.Exists(iconsDir))
            {
                foreach (string file in Directory.EnumerateFiles(iconsDir))
                {
                    iconLookup[Path.GetFileNameWithoutExtension(file)] = file;
                }
            }

            try
            {
                if (!Directory.Exists(_path))
                {
                    LogManager.Warn($"CubeBlocks directory not found at {_path}");
                    return data;
                }

                foreach (string file in Directory.GetFiles(_path, "*.sbc"))
                {
                    string blockName = string.Empty;
                    try
                    {
                        XDocument doc = XDocument.Load(file);

                        foreach (XElement definition in doc.Descendants("Definition"))
                        {
                            XElement? idElement = definition.Element("Id");
                            if (idElement != null)
                            {
                                string subtypeId = idElement.Element("SubtypeId")?.Value ?? string.Empty;
                                blockName = subtypeId;
                                Dictionary<string, int> components = [];
                                int pcu = 0;
                                string dlc = definition.Element("DLC")?.Value ?? string.Empty;
                                string gridSize = definition.Element("CubeSize")?.Value ?? string.Empty;
                                string icon = definition.Element("Icon")?.Value ?? string.Empty;
                                string iconPath = string.Empty;

                                if (!string.IsNullOrEmpty(icon))
                                {
                                    string iconFileName = Path.GetFileNameWithoutExtension(
                                        Path.Combine(Program.Config.SpaceEngineersDirectory, "Content", icon));
                                    
                                    if (iconLookup.TryGetValue(iconFileName, out string? found))
                                        iconPath = found;
                                }

                                XElement? pcuElement = definition.Element("PCU");
                                if (pcuElement != null && int.TryParse(pcuElement.Value, out int pcuValue))
                                    pcu = pcuValue;

                                foreach (XElement component in definition.Descendants("Component"))
                                {
                                    string? subtype = component.Attribute("Subtype")?.Value;
                                    if (int.TryParse(component.Attribute("Count")?.Value, out int count) && subtype != null)
                                    {
                                        if (components.ContainsKey(subtype))
                                        {
                                            components[subtype] += count;
                                        }
                                        else
                                            components[subtype] = count;
                                    }
                                }

                                if (!string.IsNullOrEmpty(subtypeId))
                                {
                                    BlockData blockData = new()
                                    {
                                        Name = ParseSubtypeName(subtypeId),
                                        DLCType = dlc,
                                        Material = components,
                                        PCU = pcu,
                                        GridSize = gridSize,
                                        IconPath = iconPath
                                    };

                                    string dirPath = Path.Combine(AppContext.BaseDirectory, "BlockData");
                                    Directory.CreateDirectory(dirPath);

                                    string filePath = Path.Combine(dirPath, $"{subtypeId}-{gridSize}-BlockData.yml");
                                    string yaml = YamlConfig.Serializer.Serialize(blockData);
                                    await File.WriteAllTextAsync(filePath, yaml);
                                    data.Add($"{subtypeId}-{gridSize}", blockData);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Error processing file {Path.GetFileName(file)} (block '{blockName}'): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"An error occurred during startup data load: {ex.Message}");
            }

            return data;
        }
    }
}