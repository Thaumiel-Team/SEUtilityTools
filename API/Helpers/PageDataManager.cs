using SEUtilityTools.API.Interface;
using SEUtilityTools.API.Yaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEUtilityTools.API.Helpers
{
    internal class PageDataManager
    {
        /// <summary>
        /// The dictionary that holds the data for each page. The key is the page, and the value is the yaml data for that page.
        /// </summary>
        public static Dictionary<string, string> DataByPage { get; private set; } = [];

        /// <summary>
        /// Saves the data if you already have a yaml serialized string, otherwise use the generic version of this method to save the data. This will overwrite any existing data for the page.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="yaml"></param>
        public static void SaveData(Page page, string yaml)
        {
            if (!DataByPage.TryAdd(page.PageName, yaml))
            {
                DataByPage[page.PageName] = yaml;
            }
        }

        /// <summary>
        /// Saves the data using the yaml serializer. This will overwrite any existing data for the page. The data must be a class that inherits from PageData, and it will be serialized to yaml before being saved in the dictionary.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page"></param>
        /// <param name="data"></param>
        public static void SaveData<T>(Page page, T data) where T : PageData
        {
            string yaml = YamlConfig.Serializer.Serialize(data);
            if (!DataByPage.TryAdd(page.PageName, yaml))
            {
                DataByPage[page.PageName] = yaml;
            }
        }

        /// <summary>
        /// Loads and deserializes YAML associated with the specified page into an instance of the given PageData derived type.  
        /// </summary>
        /// <remarks>Reads YAML from DataByPage and uses YamlConfig.Deserializer.Deserialize<T>. Returns null when no YAML is present or the YAML is empty.</remarks>
        /// <typeparam name="T">The PageData-derived type to deserialize the YAML into.</typeparam>
        /// <param name="page">The page whose associated YAML content is loaded and deserialized.</param>
        /// <returns>An instance of T if YAML exists and deserialization succeeds otherwise, null.</returns>
        public static T? LoadData<T>(Page page) where T : PageData
        {
            T? data = null;
            if (DataByPage.TryGetValue(page.PageName, out string? yaml) && !string.IsNullOrEmpty(yaml))
                data = YamlConfig.Deserializer.Deserialize<T>(yaml);

            return data;
        }
    }
}
