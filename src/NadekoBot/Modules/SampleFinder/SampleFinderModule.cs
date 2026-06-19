using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Net.Http;
using System.Text.Json;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.SampleFinder;

[Group]
public class SampleFinderModule : NadekoModule
{
    private readonly SampleFinderConfig _config;
    private readonly IHttpClientFactory _httpFactory;

    public SampleFinderModule(SampleFinderConfig config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    [Cmd]
    [Aliases("sf")]
    public async Task SampleFind([Remainder] string input)
    {
        var apiKey = _config.Data.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await ctx.Channel.SendMessageAsync("❌ Freesound API key is not configured. Use `.samplefinderconfig apikey YOUR_KEY` to set one.");
            return;
        }

        var parts = input.Split(',');
        var query = parts[0].Trim();
        var key = parts.Length > 1 ? parts[1].Trim() : null;
        int? minBpm = null, maxBpm = null;

        if (parts.Length > 2)
        {
            var bpmPart = parts[2].Trim();
            var bpmRange = bpmPart.Split('-');
            if (bpmRange.Length == 2 && int.TryParse(bpmRange[0].Trim(), out int lo) && int.TryParse(bpmRange[1].Trim(), out int hi))
            {
                minBpm = lo;
                maxBpm = hi;
            }
        }

        var searchQuery = query;
        if (key != null) searchQuery += $" {key}";

        var url = $"https://freesound.org/apiv2/search/text/?query={Uri.EscapeDataString(searchQuery)}&fields=name,url,previews,description&page_size=20&token={apiKey}";

        using var http = _httpFactory.CreateClient();
        var response = await http.GetStringAsync(url);
        var json = JsonDocument.Parse(response);
        var results = json.RootElement.GetProperty("results");

        var samples = new List<(string name, string pageLink, string previewUrl)>();
        foreach (var item in results.EnumerateArray())
        {
            var name = item.GetProperty("name").GetString() ?? "Unknown";
            var pageLink = item.GetProperty("url").GetString() ?? "";
            var previewUrl = item.GetProperty("previews").GetProperty("preview-hq-mp3").GetString() ?? "";
            var description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";

            if (minBpm != null && maxBpm != null)
            {
                bool bpmMatch = false;
                for (int b = minBpm.Value; b <= maxBpm.Value; b++)
                {
                    if (name.Contains(b.ToString()) || description.Contains(b.ToString()))
                    {
                        bpmMatch = true;
                        break;
                    }
                }
                if (!bpmMatch) continue;
            }

            samples.Add((name, pageLink, previewUrl));
            if (samples.Count >= 10) break;
        }

        if (samples.Count == 0)
        {
            await ctx.Channel.SendMessageAsync("❌ No samples found for that search.");
            return;
        }

        int index = 0;

        async Task<IUserMessage> SendSample(int i)
        {
            var (name, pageLink, previewUrl) = samples[i];

            var mp3Bytes = await http.GetByteArrayAsync(previewUrl);
            using var stream = new System.IO.MemoryStream(mp3Bytes);

            var headerText = $"**Freesound: {query}**";
            if (key != null) headerText += $" | Key: {key}";
            if (minBpm != null) headerText += $" | BPM: {minBpm}-{maxBpm}";
            headerText += $"\n**[{i + 1}/{samples.Count}] {name}**\n{pageLink}";

            var components = new ComponentBuilder()
                .WithButton("◀", "prev", ButtonStyle.Secondary, disabled: samples.Count <= 1)
                .WithButton("▶", "next", ButtonStyle.Secondary, disabled: samples.Count <= 1)
                .Build();

            return await ctx.Channel.SendFileAsync(stream, $"{name}.mp3", headerText, components: components);
        }

        var msg = await SendSample(index);

        var client = (DiscordSocketClient)ctx.Client;
        var timeout = DateTime.UtcNow.AddSeconds(60);

        client.InteractionCreated += async interaction =>
        {
            if (interaction is not SocketMessageComponent component) return;
            if (component.Message.Id != msg.Id) return;

            await component.DeferAsync();

            if (component.Data.CustomId == "next")
                index = (index + 1) % samples.Count;
            else if (component.Data.CustomId == "prev")
                index = (index - 1 + samples.Count) % samples.Count;

            await msg.DeleteAsync();
            msg = await SendSample(index);
            timeout = DateTime.UtcNow.AddSeconds(60);
        };

        while (DateTime.UtcNow < timeout)
            await Task.Delay(500);

        // Disable buttons after timeout
        var disabledComponents = new ComponentBuilder()
            .WithButton("◀", "prev", ButtonStyle.Secondary, disabled: true)
            .WithButton("▶", "next", ButtonStyle.Secondary, disabled: true)
            .Build();

        await msg.ModifyAsync(m => m.Components = disabledComponents);
    }
}
