using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SEUtilityTools.API.Yaml
{
    public static class YamlConfig
    {
        public static ISerializer Serializer
        {
            get
            {
                if (field == null)
                    Init();
                
                return field;
            }

            private set;
        }

        public static IDeserializer Deserializer
        {
            get
            {
                if (field == null)
                    Init();
                
                return field;
            }

            private set;
        }

        public static void Init()
        {
            Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDefaultScalarStyle(ScalarStyle.SingleQuoted)
            .Build();

            Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        }
    }
}