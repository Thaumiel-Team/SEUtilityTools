using SEUtilityTools.API.Yaml;

namespace SEUtilityTools.API.Interface
{
    public abstract class PageData
    {
        public string ToYaml() => YamlConfig.Serializer.Serialize(this);
    }
}
