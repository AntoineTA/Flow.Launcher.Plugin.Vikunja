using Newtonsoft.Json;

namespace Flow.Launcher.Plugin.Vikunja
{
    public class Settings
    {
        [JsonProperty("serverUrl")]
        public string ServerUrl { get; set; } = "";

        [JsonProperty("apiToken")]
        public string ApiToken { get; set; } = "";

        [JsonProperty("defaultProjectId")]
        public int DefaultProjectId { get; set; } = 1;

        [JsonProperty("parsingMode")]
        public ParsingMode ParsingMode { get; set; } = ParsingMode.Vikunja;
    }

    public enum ParsingMode
    {
        Vikunja,
        Todoist
    }
}