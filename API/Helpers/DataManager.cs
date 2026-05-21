using SEUtilityTools.API.Interface;

namespace SEUtilityTools.API.Helpers
{
    internal class DataManager
    {
        /// <summary>
        /// The dictionary that holds the data for each page. The key is the page, and the value is the yaml data for that page.
        /// </summary>
        public static Dictionary<string, PageData> DataByPage { get; private set; } = [];

        /// <summary>
        /// This will overwrite any existing data for the page. The data must be a class that inherits from PageData, and it will be serialized to yaml before being saved in the dictionary.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page"></param>
        /// <param name="data"></param>
        public static void SaveData<T>(Page page, T data) where T : PageData
        {
            if (!DataByPage.TryAdd(page.PageName, data))
            {
                DataByPage[page.PageName] = data;
            }
        }

        /// <summary>
        /// Loads the data associated with the specified page into an instance of the given PageData derived type.  
        /// </summary>
        /// <typeparam name="T">The PageData-derived type to deserialize the YAML into.</typeparam>
        /// <param name="page">The page whose associated YAML content is loaded and deserialized.</param>
        /// <returns>An instance of T if YAML exists and deserialization succeeds otherwise, null.</returns>
        public static T? LoadData<T>(Page page) where T : PageData
        {
            T? data = null;
            if (DataByPage.TryGetValue(page.PageName, out PageData? pageData))
                data = pageData as T;

            return data;
        }

        public static async Task<bool> SaveToFileAsync(Page page, string filename, string path)
        {
            if (!DataByPage.TryGetValue(page.PageName, out var data))
                return false;

            await File.WriteAllTextAsync(Path.Combine(path, filename), data.ToYaml());
            return true;
        }

        public static async Task<bool> SaveToFileAsync(Page page, string filename)
            => await SaveToFileAsync(page, filename, AppContext.BaseDirectory);

        public static async Task<bool> SaveToFileAsync(Page page)
            => await SaveToFileAsync(page, $"{page.PageName}-Data.yml", AppContext.BaseDirectory);

        public static bool SaveToFile(Page page, string filename, string path)
        {
            if (!DataByPage.TryGetValue(page.PageName, out var data))
                return false;

            File.WriteAllText(Path.Combine(path, filename), data.ToYaml());
            return true;
        }

        public static bool SaveToFile(Page page, string filename)
            => SaveToFile(page, filename, AppContext.BaseDirectory);

        public static bool SaveToFile(Page page)
            => SaveToFile(page, $"{page.PageName}-Data.yml", AppContext.BaseDirectory);
    }
}