using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SEUtilityTools.API.Yaml;

namespace SEUtilityTools.API.Helpers
{
    public static class ConfigManager
    {
        public static string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.yml");

        public static Config? Config
        {
            get
            {
                if (field == null)
                    LoadConfig();

                return field;
            }

            private set;
        }

        public static void Init()
        {
            if (!File.Exists(ConfigPath))
            {
                SaveDefaultConfig();
            }
            else
            {
                LoadConfig();
            }

            if (MergeDefaults())
                SaveConfig();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                SaveDefaultConfig();
                return;
            }

            string text = File.ReadAllText(ConfigPath);
            if (string.IsNullOrWhiteSpace(text))
            {
                SaveDefaultConfig();
                return;
            }

            try
            {
                Config = YamlConfig.Deserializer.Deserialize<Config>(text) ?? new();
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to read config: {ex}");
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigPath, YamlConfig.Serializer.Serialize(Config));
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to save config: {ex}");
            }
        }

        public static void SaveDefaultConfig()
        {
            if (!Directory.Exists(Config!.SpaceEngineersDirectory))
            {
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("The Space Engineers steam directory wasn't found!");
                Console.WriteLine($"Default: {Config.SpaceEngineersDirectory}");
                Console.Write("Please input the full directory (or press Enter to keep default): ");
                
                string? input = Console.ReadLine();
                
                if (!string.IsNullOrWhiteSpace(input))
                    Config.SpaceEngineersDirectory = input;
            }

            SaveConfig();
        }

        public static bool MergeDefaults()
        {
            bool changed = false;
            Config defaults = new();

            foreach (PropertyInfo prop in typeof(Config).GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;
                    
                object? current = prop.GetValue(Config);
                object? fallback = prop.GetValue(defaults);
                
                if (current == null && fallback != null)
                {
                    prop.SetValue(Config, fallback);
                    changed = true;
                }
            }

            return changed;
        }

        public static bool EnsureKey(string key, object defaultValue)
        {
            PropertyInfo? prop = typeof(Config).GetProperty(key);
            if (prop == null || !prop.CanRead || !prop.CanWrite)
                return false;

            if (prop.GetValue(Config) == null)
            {
                prop.SetValue(Config, defaultValue);
                SaveConfig();
                return true;
            }

            return false;
        }
    }
}