using System.Text.Json.Serialization;

namespace DataRequestPipeline.Core.Configuration
{
    public class PluginConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("plugins")]
        public List<string> Plugins { get; set; } = new List<string>();
    }
}
