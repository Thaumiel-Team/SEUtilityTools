using System.Collections.Concurrent;
using System.Xml.Linq;
using SEUtilityTools.API.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Platform;

namespace SEUtilityTools.API.Helpers
{
    public static class BlueprintManager
    {
        private static string _path = string.Empty;
        public static List<BlueprintData> Blueprints { get; private set; } = [];

        private static readonly WriteableBitmap _emptyBitmap = CreateEmptyBitmap();

        public static async Task Init(bool promptOnLarge = true, CancellationToken ct = default, IProgress<int>? progress = null)
        {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers", "Blueprints", "local");

            if (!Directory.Exists(_path))
            {
                await MessageBoxManager.GetMessageBoxStandard("Directory Not Found", $"No blueprints found at {_path}", ButtonEnum.Ok, Icon.Error).ShowAsync();
                return;
            }

            Blueprints = await Load(promptOnLarge, ct, progress).ConfigureAwait(false);
        }

        public static async Task<List<BlueprintData>> Load(bool promptOnLarge = true, CancellationToken ct = default, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(_path))
                throw new InvalidOperationException("BlueprintManager has not been initialized. Call Init() first.");

            string[] folders = Directory.EnumerateDirectories(_path).ToArray();

            if (folders.Length == 0)
            {
                await MessageBoxManager.GetMessageBoxStandard(string.Empty, "No Blueprints Were Found.", ButtonEnum.Ok, Icon.Info).ShowAsync();
                return [];
            }

            if (promptOnLarge && folders.Length > 100)
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Large Blueprint Collection",$"There are {folders.Length} blueprints. Loading them all may take a while.\r\nDo you want to continue?", ButtonEnum.YesNo, Icon.Warning).ShowAsync();
                if (result == ButtonResult.No)
                    return [];
            }

            Dictionary<string, BlockData> exactBlocks = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, BlockData> subtypeBlocks = new(StringComparer.OrdinalIgnoreCase);

            if (BlockManager.Blocks is not null)
            {
                foreach (KeyValuePair<string, BlockData> kv in BlockManager.Blocks)
                {
                    exactBlocks[kv.Key] = kv.Value;

                    string key = kv.Key;
                    int dash = key.LastIndexOf('-');
                    if (dash > 0)
                    {
                        string subtype = key.Substring(0, dash);
                        subtypeBlocks.TryAdd(subtype, kv.Value);
                    }
                    else
                    {
                        subtypeBlocks.TryAdd(key, kv.Value);
                    }
                }
            }

            string fallbackIcon = Path.Combine(AppContext.BaseDirectory, "Data", "IconsPNG", "no_image.png");
            ConcurrentBag<BlueprintData> results = [];
            int processed = 0;

            await Parallel.ForEachAsync(folders, new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
                CancellationToken = ct
            }, async (folder, ct) =>
            {
                List<BlockData> blockDataList = [];
                Bitmap? icon = null;

                try
                {
                    ct.ThrowIfCancellationRequested();

                    string thumbPath = Path.Combine(folder, "thumb.png");
                    string? imgPath = File.Exists(thumbPath) ? thumbPath : (File.Exists(fallbackIcon) ? fallbackIcon : null);

                    if (imgPath is not null)
                    {
                        try
                        {
                            using FileStream fs = new(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            icon = new Bitmap(fs);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Warn($"Failed to load thumbnail for '{folder}': {ex.Message}");
                        }
                    }

                    string bpFile = Path.Combine(folder, "bp.sbc");
                    if (!File.Exists(bpFile))
                    {
                        LogManager.Warn($"Blueprint '{folder}' has no bp.sbc file");
                        return;
                    }

                    await using FileStream xmlFs = new(bpFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
                    using StreamReader xmlReader = new(xmlFs);

                    foreach (XElement cubeGrid in (await XDocument.LoadAsync(xmlReader, LoadOptions.None, ct).ConfigureAwait(false)).Descendants().Where(x => string.Equals(x.Name.LocalName, "CubeGrid", StringComparison.OrdinalIgnoreCase)))
                    {
                        string gridSize = GetChildValue(cubeGrid, "GridSizeEnum") ?? "Large";

                        foreach (XElement block in cubeGrid.Descendants().Where(x => string.Equals(x.Name.LocalName, "MyObjectBuilder_CubeBlock", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name.LocalName, "CubeBlock", StringComparison.OrdinalIgnoreCase)))
                        {
                            string subtypeName = GetChildValue(block, "SubtypeName") ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(subtypeName))
                                continue;

                            string exactKey = $"{subtypeName}-{gridSize}";

                            if (!exactBlocks.TryGetValue(exactKey, out BlockData? blockData))
                            {
                                subtypeBlocks.TryGetValue(subtypeName, out blockData);
                            }

                            if (blockData is not null)
                            {
                                blockDataList.Add(blockData);
                            }
                        }
                    }

                    BlueprintData data = new()
                    {
                        Folder = folder,
                        Name = Path.GetFileName(folder) ?? folder,
                        Icon = icon ?? _emptyBitmap,
                        Blocks = blockDataList
                    };

                    results.Add(data);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Failed to load blueprint '{folder}': {ex}");
                }
                finally
                {
                    progress?.Report(Interlocked.Increment(ref processed));
                }
            }).ConfigureAwait(false);

            return results.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void Clear()
        {
            if (Blueprints == null)
                return;

            foreach (BlueprintData bp in Blueprints)
            {
                if (bp.Icon is IDisposable disposable && !ReferenceEquals(bp.Icon, _emptyBitmap))
                    disposable.Dispose();
            }

            Blueprints.Clear();
        }

        private static WriteableBitmap CreateEmptyBitmap()
        {
            WriteableBitmap bitmap = new(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
            using ILockedFramebuffer fb = bitmap.Lock();
            unsafe
            {
                *(uint*)fb.Address = 0x00000000;
            }

            return bitmap;
        }

        private static string? GetChildValue(XElement parent, string childLocalName) =>
            parent.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, childLocalName, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}