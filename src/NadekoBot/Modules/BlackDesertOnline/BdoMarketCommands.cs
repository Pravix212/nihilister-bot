using NadekoBot.Modules.BlackDesertOnline.Models;

namespace NadekoBot.Modules.BlackDesertOnline;

public partial class Bdo
{
    [Cmd]
    public async Task BdoMarket([Leftover] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await Response().Error("Please provide an item name. Example: `.bdomarket Caphras Stone`").SendAsync();
            return;
        }

        await ShowMarketAsync("na", query);
    }

    private async Task ShowMarketAsync(string region, string query, long specificId = 0, int specificSid = 0)
    {
        await ctx.Channel.TriggerTypingAsync();

        if (specificId > 0)
        {
            var item = await _mkt.GetItemAsync(region, specificId, specificSid);
            if (item is null)
            {
                await Response().Error("Could not fetch item data from the market API.").SendAsync();
                return;
            }
            await SendMarketEmbedAsync(region, item, query);
        }
        else
        {
            var results = await _mkt.SearchItemsAsync(region, query);

            if (results is null || results.Count == 0)
            {
                await Response().Error($"No items found for **{query}** on the {BdoMarketService.RegionNames[region]} market.\n-# Market data is cached ~30 minutes.").SendAsync();
                return;
            }

            if (results.Count == 1)
            {
                var item = await _mkt.GetItemAsync(region, results[0].Id, results[0].Sid);
                if (item is null)
                {
                    await Response().Error("Could not fetch item data from the market API.").SendAsync();
                    return;
                }
                await SendMarketEmbedAsync(region, item, query);
            }
            else
            {
                await ShowItemSelectionAsync(region, query, results);
            }
        }
    }

    private async Task ShowItemSelectionAsync(string region, string query, List<ArshiaSearchResult> results)
    {
        var top = results.Take(25).ToList();
        var menu = new SelectMenuBuilder()
            .WithCustomId($"bdo_item_select:{region}")
            .WithPlaceholder("Select an item...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var r in top)
            menu.AddOption(r.Name.Length > 100 ? r.Name[..97] + "..." : r.Name,
                           $"{r.Id}:{r.Sid}");

        var selectInteraction = _inter.Create(
            ctx.User.Id,
            menu,
            async (smc) =>
            {
                var val = smc.Data.Values.First().Split(':');
                var id = long.Parse(val[0]);
                var sid = int.Parse(val[1]);
                await smc.DeferAsync();
                var item = await _mkt.GetItemAsync(region, id, sid);
                if (item is null) return;
                var eb = BuildMarketEmbed(region, item);
                await smc.Message.ModifyAsync(m =>
                {
                    m.Embed = eb.Build();
                    m.Components = new ComponentBuilder()
                        .WithSelectMenu(BuildRegionDropdown(region, item.Id, item.Sid, query))
                        .Build();
                });
            },
            singleUse: false);

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle($"🔍 Market Search: {query}")
            .WithDescription($"Found **{results.Count}** items. Select one to view details.");

        await Response().Embed(eb).Interactions(selectInteraction).SendAsync();
    }

    private async Task SendMarketEmbedAsync(string region, ArshiaMarketItem item, string query)
    {
        var eb = BuildMarketEmbed(region, item);
        var regionMenu = _inter.Create(
            ctx.User.Id,
            BuildRegionDropdown(region, item.Id, item.Sid, query),
            async (smc) =>
            {
                var newRegion = smc.Data.Values.First();
                await smc.DeferAsync();
                var newItem = await _mkt.GetItemAsync(newRegion, item.Id, item.Sid);
                if (newItem is null) return;
                var newEb = BuildMarketEmbed(newRegion, newItem);
                await smc.Message.ModifyAsync(m =>
                {
                    m.Embed = newEb.Build();
                    m.Components = new ComponentBuilder()
                        .WithSelectMenu(BuildRegionDropdown(newRegion, newItem.Id, newItem.Sid, query))
                        .Build();
                });
            },
            singleUse: false);

        await Response().Embed(eb).Interactions(regionMenu).SendAsync();
    }

    private EmbedBuilder BuildMarketEmbed(string region, ArshiaMarketItem item)
    {
        var regionLabel = BdoMarketService.RegionNames.TryGetValue(region, out var rn) ? rn : region.ToUpper();
        var lastSold = _mkt.FormatLastSoldTime(item.LastSoldTime);

        var desc = item.CurrentStock == 0
            ? $"📭 **OUT OF STOCK** on {regionLabel}\n-# Data cached ~30 min · via arsha.io"
            : $"Market data for **{regionLabel}**\n-# Data cached ~30 min · via arsha.io";

        return CreateEmbed()
            .WithOkColor()
            .WithTitle($"📦 {item.Name}")
            .WithDescription(desc)
            .AddField("💰 Price Range",
                $"`{_mkt.FormatSilver(item.PriceMin)}` — `{_mkt.FormatSilver(item.PriceMax)}`",
                true)
            .AddField("📦 Listed Stock", $"`{item.CurrentStock:N0}`", true)
            .AddField("🔄 Total Trades", $"`{item.TotalTrades:N0}`", true)
            .AddField("💸 Last Sold Price", $"`{_mkt.FormatSilver(item.LastSoldPrice)}`", true)
            .AddField("🕐 Last Sold", lastSold, true);
    }

    private SelectMenuBuilder BuildRegionDropdown(string currentRegion, long itemId, int sid, string query)
    {
        var currentLabel = BdoMarketService.RegionNames.TryGetValue(currentRegion, out var rn) ? rn : currentRegion.ToUpper();
        var menu = new SelectMenuBuilder()
            .WithCustomId($"bdo_region:{itemId}:{sid}:{Uri.EscapeDataString(query)}")
            .WithPlaceholder($"🌍 Region: {currentLabel}")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var (key, val) in BdoMarketService.RegionNames)
            menu.AddOption(val, key, isDefault: key == currentRegion);

        return menu;
    }
}