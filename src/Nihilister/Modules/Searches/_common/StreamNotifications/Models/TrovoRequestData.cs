#nullable disable
using System.Text.Json.Serialization;

namespace Nihilister.Modules.Searches.Common.StreamNotifications.Providers;

public class TrovoRequestData
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
}