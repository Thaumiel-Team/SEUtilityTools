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

        public static async Task Init(bool promptOnLarge = true, CancellationToken ct = default)
        {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers", "Blueprints", "local");
            if (!Directory.Exists(_path))
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Directory Not Found", $"No blueprints found at {_path}", ButtonEnum.Ok, Icon.Error)
                    .ShowAsync();
                return;
            }

            Blueprints = await Load(promptOnLarge, ct).ConfigureAwait(false);
        }

        public static async Task<List<BlueprintData>> Load(bool promptOnLarge = true, CancellationToken ct = default, IProgress<int>? progress = null)
        {
            ConcurrentBag<BlueprintData> listBag = [];
            string[]? folders = Directory.EnumerateDirectories(_path).ToArray();
            if (folders.Length == 0)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard(string.Empty, "No Blueprints Were Found.", ButtonEnum.Ok, Icon.Info)
                    .ShowAsync();
                return [];
            }

            if (promptOnLarge && folders.Length > 100)
            {
                ButtonResult result = await MessageBoxManager
                    .GetMessageBoxStandard("Large Blueprint Collection", $"There are {folders.Length} blueprints. Loading them all may take a while.\r\nDo you want to continue?", ButtonEnum.YesNo, Icon.Warning)
                    .ShowAsync();

                if (result == ButtonResult.No)
                    return [];
            }

            int maxDegree = Math.Max(1, Environment.ProcessorCount);
            using SemaphoreSlim semaphore = new(maxDegree);
            List<Task> tasks = [];
            int processed = 0;
            string fallbackIcon = Path.Combine(AppContext.BaseDirectory, "Data", "IconsPNG", "no_image.png");

            static string? GetChildValue(XElement parent, string childLocalName) =>
                parent.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, childLocalName, StringComparison.OrdinalIgnoreCase))?.Value;

            foreach (string folder in folders)
            {
                await semaphore.WaitAsync(ct).ConfigureAwait(false);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        string thumbPath = Path.Combine(folder, "thumb.png");
                        string imgPath = File.Exists(thumbPath) ? thumbPath : fallbackIcon;

                        Bitmap? icon = null;
                        if (File.Exists(imgPath))
                        {
                            try
                            {
                                byte[]? bytes = await File.ReadAllBytesAsync(imgPath, ct).ConfigureAwait(false);
                                using MemoryStream ms = new(bytes);
                                icon = new Bitmap(ms);
                            }
                            catch { }
                        }

                        List<BlockData> blockDataList = [];
                        string bpFile = Path.Combine(folder, "bp.sbc");
                        if (!File.Exists(bpFile))
                        {
                            LogManager.Warn($"Blueprint '{folder}' has no bp.sbc file");
                            return;
                        }

                        string xmlContent = await File.ReadAllTextAsync(bpFile, ct).ConfigureAwait(false);
                        XDocument doc = XDocument.Parse(xmlContent);

                        IEnumerable<XElement> cubeGrids = doc.Descendants().Where(x => string.Equals(x.Name.LocalName, "CubeGrid", StringComparison.OrdinalIgnoreCase));
                        foreach (XElement cubeGrid in cubeGrids)
                        {
                            string gridSize = GetChildValue(cubeGrid, "GridSizeEnum") ?? "Large";

                            IEnumerable<XElement> cubeBlocks = cubeGrid.Descendants().Where(x => string.Equals(x.Name.LocalName, "MyObjectBuilder_CubeBlock", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name.LocalName, "CubeBlock", StringComparison.OrdinalIgnoreCase));
                            foreach (XElement block in cubeBlocks)
                            {
                                string subtypeName = GetChildValue(block, "SubtypeName") ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(subtypeName))
                                    continue;

                                BlockData? blockData = null;
                                if (BlockManager.Blocks != null)
                                {
                                    if (!BlockManager.Blocks.TryGetValue($"{subtypeName}-{gridSize}", out blockData))
                                    {
                                        KeyValuePair<string, BlockData> kv = BlockManager.Blocks.FirstOrDefault(k => string.Equals(k.Key, subtypeName, StringComparison.OrdinalIgnoreCase));
                                        if (!kv.Equals(default(KeyValuePair<string, BlockData>)))
                                            blockData = kv.Value;
                                    }
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
                            Icon = icon ?? CreateEmptyBitmap(),
                            Blocks = blockDataList
                        };

                        listBag.Add(data);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        LogManager.Warn($"Failed to load blueprint '{folder}': {ex}");
                    }
                    finally
                    {
                        int value = Interlocked.Increment(ref processed);
                        progress?.Report(value);
                        semaphore.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            List<BlueprintData> resultList = listBag.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
            return resultList;
        }

        public static void Clear()
        {
            if (Blueprints == null)
                return;

            foreach (BlueprintData bp in Blueprints)
            {
                if (bp.Icon is IDisposable disposable)
                    disposable.Dispose();
            }

            Blueprints.Clear();
        }

        private static WriteableBitmap CreateEmptyBitmap()
        {
            WriteableBitmap bitmap = new(
                new PixelSize(1, 1),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Opaque);

            using ILockedFramebuffer fb = bitmap.Lock();
            unsafe
            {
                *(uint*)fb.Address = 0x00000000;
            }

            return bitmap;
        }
    }
}