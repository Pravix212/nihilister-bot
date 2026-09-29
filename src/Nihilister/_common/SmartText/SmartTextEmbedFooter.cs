#nullable disable
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Nihilister;

public class SmartTextEmbedFooter
{
    public string Text { get; set; }

    [JsonProperty("icon_url")]
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }
}