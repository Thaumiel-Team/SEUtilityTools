using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using BCnEncoder.Decoder;
using BCnEncoder.Shared.ImageFiles;
using SkiaSharp;

namespace SEUtilityTools.API.Helpers
{
    public static class ImageConverter
    {
        private static readonly ConcurrentDictionary<string, byte[]> _convertedImages = new();
        private static readonly BcDecoder _decoder = new();
        public static IReadOnlyDictionary<string, byte[]> ConvertedImages => _convertedImages;

        public static async Task<bool> ConvertDdsToPngAsync(string ddsFilePath, string outputDir = "ConvertedIcons")
        {
            try
            {
                if (!File.Exists(ddsFilePath))
                {
                    LogManager.Error($"DDS file not found: {ddsFilePath}");
                    return false;
                }

                string fileName = Path.GetFileNameWithoutExtension(ddsFilePath);
                Directory.CreateDirectory(outputDir);
                string outputPath = Path.Combine(outputDir, $"{fileName}.png");

                if (_convertedImages.TryGetValue(fileName, out byte[]? cached) && cached is not null)
                {
                    if (!File.Exists(outputPath))
                        await File.WriteAllBytesAsync(outputPath, cached).ConfigureAwait(false);
                        
                    return true;
                }

                if (File.Exists(outputPath))
                {
                    byte[] existingPng = await File.ReadAllBytesAsync(outputPath).ConfigureAwait(false);
                    _convertedImages[fileName] = existingPng;
                    return true;
                }

                byte[] ddsData = await File.ReadAllBytesAsync(ddsFilePath).ConfigureAwait(false);
                byte[] pngData = await Task.Run(() =>
                {
                    using MemoryStream ddsStream = new(ddsData, writable: false);
                    DdsFile ddsFile = DdsFile.Load(ddsStream);
                    BCnEncoder.Shared.ColorRgba32[] decodedPixels = _decoder.Decode(ddsFile);

                    int width = (int)ddsFile.header.dwWidth;
                    int height = (int)ddsFile.header.dwHeight;

                    ReadOnlySpan<byte> pixelBytes = MemoryMarshal.AsBytes(decodedPixels.AsSpan());

                    SKImageInfo imageInfo = new(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                    using SKImage image = SKImage.FromPixelCopy(imageInfo, pixelBytes) ?? throw new InvalidOperationException("Failed to create SkiaSharp image from decoded pixels.");
                    using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);

                    return encoded is null ? throw new InvalidOperationException("Failed to encode image to PNG.") : encoded.ToArray();
                }).ConfigureAwait(false);

                await File.WriteAllBytesAsync(outputPath, pngData).ConfigureAwait(false);
                _convertedImages[fileName] = pngData;
                LogManager.Info($"Successfully converted {fileName} from DDS to PNG");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to convert DDS file {ddsFilePath}: {ex.Message}");
                return false;
            }
        }

        public static async Task<int> ConvertDirectoryAsync(string directoryPath, string searchPattern = "*.dds", bool recursive = false, string outputDir = "ConvertedIcons")
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    LogManager.Error($"Directory not found: {directoryPath}");
                    return 0;
                }

                SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                string[] ddsFiles = Directory.GetFiles(directoryPath, searchPattern, searchOption);
                LogManager.Info($"Found {ddsFiles.Length} DDS files in {directoryPath}");
                Directory.CreateDirectory(outputDir);

                bool allExist = true;
                foreach (string ddsPath in ddsFiles)
                {
                    string expectedPng = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(ddsPath)}.png");
                    if (!File.Exists(expectedPng))
                    {
                        allExist = false;
                        break;
                    }
                }
                
                if (allExist && ddsFiles.Length > 0)
                {
                    LogManager.Info($"Skipping conversion: all {ddsFiles.Length} PNG(s) already exist in '{outputDir}'.");
                    return 0;
                }

                List<Task<bool>> tasks = new(ddsFiles.Length);
                foreach (string ddsPath in ddsFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(ddsPath);
                    string expectedPng = Path.Combine(outputDir, $"{fileName}.png");

                    if (File.Exists(expectedPng))
                        continue;

                    tasks.Add(ConvertDdsToPngAsync(ddsPath, outputDir));
                }

                if (tasks.Count == 0)
                {
                    LogManager.Info("No missing PNGs to convert.");
                    return 0;
                }

                bool[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
                int converted = results.Count(r => r);
                LogManager.Info($"Converted {converted}/{ddsFiles.Length} DDS files.");
                return converted;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to convert directory {directoryPath}: {ex.Message}");
                return 0;
            }
        }

        public static byte[]? GetConvertedImage(string fileName)
        {
            if (_convertedImages.TryGetValue(fileName, out byte[]? data))
                return data;

            LogManager.Warn($"Converted image not found: {fileName}");
            return null;
        }

        public static void ClearCache()
        {
            int count = _convertedImages.Count;
            _convertedImages.Clear();
            LogManager.Debug($"Cleared {count} images from cache");
        }
    }
}