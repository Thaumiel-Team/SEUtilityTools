using SEUtilityTools.API.Data;
using SEUtilityTools.API.Yaml;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SEUtilityTools.API.Helpers
{
    public static class BlockManager
    {
        private static readonly Regex _camelCaseRegex = new("(?<!^)([A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string _path = Path.Combine(Program.Config.SpaceEngineersDirectory, "Content", "Data", "CubeBlocks");
        public static Dictionary<string, BlockData> Blocks { get; private set; } = [];

        public static async Task Init()
        {
            Blocks = await GetData().ConfigureAwait(false);
        }

        private static string ParseSubtypeName(string subtypeId)
        {
            if (string.IsNullOrEmpty(subtypeId))
                return subtypeId;

            return _camelCaseRegex.Replace(subtypeId, " $1");
        }

        private static async Task<Dictionary<string, BlockData>> GetData()
        {
            Dictionary<string, BlockData> data = [];

            string iconsDir = Path.Combine(AppContext.BaseDirectory, "ConvertedIcons");
            Dictionary<string, string> iconLookup = new(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(iconsDir))
            {
                foreach (string file in Directory.EnumerateFiles(iconsDir, "*", SearchOption.AllDirectories))
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    if (!iconLookup.TryAdd(key, file))
                    {
                        LogManager.Warn($"Duplicate icon key '{key}' found. Keeping: {iconLookup[key]}, Ignoring: {file}");
                    }
                }
            }

            string dirPath = Path.Combine(AppContext.BaseDirectory, "BlockData");
            Directory.CreateDirectory(dirPath);

            try
            {
                if (!Directory.Exists(_path))
                {
                    LogManager.Warn($"CubeBlocks directory not found at {_path}");
                    return data;
                }

                foreach (string file in Directory.EnumerateFiles(_path, "*.sbc"))
                {
                    string blockName = string.Empty;
                    try
                    {
                        XDocument doc;
                        await using (FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                        using (XmlReader reader = XmlReader.Create(fs, new XmlReaderSettings
                        {
                            Async = true,
                            IgnoreWhitespace = true,
                            IgnoreComments = true
                        }))
                        {
                            doc = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
                        }

                        foreach (XElement definition in doc.Descendants("Definition"))
                        {
                            XElement? idElement = definition.Element("Id");
                            if (idElement is null)
                                continue;

                            string subtypeId = idElement.Element("SubtypeId")?.Value ?? string.Empty;
                            blockName = subtypeId;

                            if (string.IsNullOrEmpty(subtypeId))
                                continue;

                            Dictionary<string, int> components = [];
                            int pcu = 0;
                            string dlc = definition.Element("DLC")?.Value ?? string.Empty;
                            string gridSize = definition.Element("CubeSize")?.Value ?? string.Empty;
                            string icon = definition.Element("Icon")?.Value ?? string.Empty;
                            string iconPath = string.Empty;

                            if (!string.IsNullOrEmpty(icon))
                            {
                                string iconName = Path.GetFileNameWithoutExtension(icon).Replace('\\', '/');
                                iconName = Path.GetFileNameWithoutExtension(iconName);
                                
                                if (iconLookup.TryGetValue(iconName, out string? found))
                                {
                                    iconPath = found;
                                }
                                else
                                    LogManager.Debug($"Icon not found for block '{subtypeId}': '{icon}' (searched for '{iconName}')");
                            }

                            XElement? pcuElement = definition.Element("PCU");
                            if (pcuElement is not null && int.TryParse(pcuElement.Value, out int pcuValue))
                                pcu = pcuValue;

                            foreach (XElement component in definition.Descendants("Component"))
                            {
                                string? subtype = component.Attribute("Subtype")?.Value;
                                if (int.TryParse(component.Attribute("Count")?.Value, out int count) && subtype is not null)
                                {
                                    if (components.TryGetValue(subtype, out int existing))
                                    {
                                        components[subtype] = existing + count;
                                    }
                                    else
                                        components[subtype] = count;
                                }
                            }

                            BlockData blockData = new()
                            {
                                Name = ParseSubtypeName(subtypeId),
                                DLCType = dlc,
                                Material = components,
                                PCU = pcu,
                                GridSize = gridSize,
                                IconPath = iconPath
                            };

                            await File.WriteAllTextAsync(Path.Combine(dirPath, $"{subtypeId}-{gridSize}-BlockData.yml"), YamlConfig.Serializer.Serialize(blockData)).ConfigureAwait(false);
                            data[$"{subtypeId}-{gridSize}"] = blockData;
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