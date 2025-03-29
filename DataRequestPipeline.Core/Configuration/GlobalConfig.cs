using System.Text.Json.Serialization;

namespace DataRequestPipeline.Core.Configuration
{
    public class GlobalConfig
    {
        [JsonPropertyName("ConnectionStrings")]
        public Dictionary<string, string> ConnectionStrings { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("Logging")]
        public LoggingConfig Logging { get; set; } = new LoggingConfig();
    }

    public class LoggingConfig
    {
        [JsonPropertyName("Verbosity")]
        public string Verbosity { get; set; } = "Info";
    }
}
