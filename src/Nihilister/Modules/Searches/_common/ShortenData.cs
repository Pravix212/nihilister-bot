#nullable disable
using Newtonsoft.Json;

namespace Nihilister.Modules.Searches.Services;

public class ShortenData
{
    [JsonProperty("result_url")]
    public string ResultUrl { get; set; }
}