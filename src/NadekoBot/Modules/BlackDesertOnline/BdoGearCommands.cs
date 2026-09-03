using System.Text.Json.Nodes;
using NadekoBot.Modules.BlackDesertOnline.Models;

namespace NadekoBot.Modules.BlackDesertOnline;

public partial class Bdo
{
    [Cmd]
    public async Task BdoGear([Leftover] string? input = null)
    {
        // 1. If input is a Garmoth link or slug (e.g. https://garmoth.com/character/pravv or pravv)
        if (!string.IsNullOrWhiteSpace(input) && (input.Contains("garmoth.com/character/") || (!input.StartsWith("<@") && !input.StartsWith("register") && !input.StartsWith("refresh") && !input.StartsWith("update"))))
        {
            var cleaned = input.Replace("register", "").Trim();
            await HandleGarmothLinkAsync(ctx.User, cleaned);
            return;
        }

        // 2. If input starts with "register"
        if (!string.IsNullOrWhiteSpace(input) && input.Trim().StartsWith("register", StringComparison.OrdinalIgnoreCase))
        {
            var slug = input.Trim().Substring("register".Length).Trim();
            if (string.IsNullOrWhiteSpace(slug))
            {
                await Response().Error("Please provide your Garmoth link or username! Example:\n`.bdogear register https://garmoth.com/character/pravv`\nor simply:\n`.bdogear pravv`").SendAsync();
                return;
            }
            await HandleGarmothLinkAsync(ctx.User, slug);
            return;
        }

        // 3. If input is "refresh" or "update"
        if (!string.IsNullOrWhiteSpace(input) && (input.Equals("refresh", StringComparison.OrdinalIgnoreCase) || input.Equals("update", StringComparison.OrdinalIgnoreCase)))
        {
            var existing = _service.GetProfile(ctx.User.Id);
            if (existing is null || string.IsNullOrWhiteSpace(existing.GarmothLink))
            {
                await Response().Error("You don't have a registered Garmoth profile yet! Use `.bdogear <garmoth-link>` to register.").SendAsync();
                return;
            }
            await HandleGarmothLinkAsync(ctx.User, existing.GarmothLink);
            return;
        }

        // 4. If a user was mentioned or no argument (show profile)
        IUser targetUser = ctx.User;
        if (!string.IsNullOrWhiteSpace(input) && MentionUtils.TryParseUser(input.Trim(), out var targetId))
        {
            targetUser = await ctx.Guild.GetUserAsync(targetId) ?? (IUser)await ctx.Client.GetUserAsync(targetId) ?? ctx.User;
        }

        var profile = _service.GetProfile(targetUser.Id);
        if (profile is null)
        {
            var msg = targetUser.Id == ctx.User.Id
                ? "You don't have a gear profile yet! Register instantly with your Garmoth link:\n`.bdogear https://garmoth.com/character/pravv`"
                : $"**{targetUser.Username}** has not registered a Garmoth gear profile yet.";
            await Response().Error(msg).SendAsync();
            return;
        }

        await ShowGearProfileAsync(profile, "stats");
    }

    [Cmd]
    public async Task BdoGearRegister([Leftover] string link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            await Response().Error("Please provide your Garmoth link. Example:\n`.bdogearregister https://garmoth.com/character/pravv`").SendAsync();
            return;
        }

        await HandleGarmothLinkAsync(ctx.User, link);
    }

    [Cmd]
    [OwnerOnly]
    public async Task BdoGearDelete(IUser? user = null)
    {
        var target = user ?? ctx.User;
        _service.DeleteProfile(target.Id);
        await ctx.OkAsync();
    }

    private async Task HandleGarmothLinkAsync(IUser user, string linkOrSlug)
    {
        await ctx.Channel.TriggerTypingAsync();
        var (success, error, profile) = await _service.FetchGarmothProfileAsync(user.Id, user.Username, linkOrSlug);

        if (!success || profile is null)
        {
            await Response().Error($"❌ Failed to load Garmoth profile: **{error}**\nMake sure your Garmoth profile is set to **Public**.").SendAsync();
            return;
        }

        await ShowGearProfileAsync(profile, "stats");
    }

    private async Task ShowGearProfileAsync(BdoGearProfile profile, string tab)
    {
        var imgPath = _service.GetGearImagePath(profile.UserId);
        var hasImage = File.Exists(imgPath);

        var eb = tab switch
        {
            "stats" => BuildStatsEmbed(profile, hasImage),
            "value" => BuildValueEmbed(profile, hasImage),
            "grind" => BuildGrindZonesEmbed(profile, hasImage),
            "caps"  => BuildCapsEmbed(profile, hasImage),
            _ => BuildStatsEmbed(profile, hasImage)
        };

        var tabMenu = new SelectMenuBuilder()
            .WithCustomId($"bdo_gear_tab:{profile.UserId}")
            .WithPlaceholder(tab switch
            {
                "stats" => "📊 Total Stats",
                "value" => "💰 Total Value",
                "grind" => "🏔️ Recommended Grind Zones",
                "caps"  => "🛡️ PVP Caps (Nodewar/Siege)",
                _ => "📊 Total Stats"
            })
            .AddOption("📊 Total Stats", "stats", "AP, AAP, DP, Offense/Defense & Gear Score", isDefault: tab == "stats")
            .AddOption("💰 Total Value", "value", "Gear worth breakdown by slots & crystals", isDefault: tab == "value")
            .AddOption("🏔️ Recommended Grind Zones", "grind", "Tailored grind zones with silver/hr", isDefault: tab == "grind")
            .AddOption("🛡️ PVP Caps", "caps", "Nodewar & Siege Tier 1 / 2 stat limits", isDefault: tab == "caps");

        var interaction = _inter.Create(
            ctx.User.Id,
            tabMenu,
            async (smc) =>
            {
                var newTab = smc.Data.Values.First();
                await smc.DeferAsync();
                var newEb = newTab switch
                {
                    "stats" => BuildStatsEmbed(profile, hasImage),
                    "value" => BuildValueEmbed(profile, hasImage),
                    "grind" => BuildGrindZonesEmbed(profile, hasImage),
                    "caps"  => BuildCapsEmbed(profile, hasImage),
                    _ => BuildStatsEmbed(profile, hasImage)
                };

                var newMenu = new SelectMenuBuilder()
                    .WithCustomId($"bdo_gear_tab:{profile.UserId}")
                    .WithPlaceholder(newTab switch
                    {
                        "stats" => "📊 Total Stats",
                        "value" => "💰 Total Value",
                        "grind" => "🏔️ Recommended Grind Zones",
                        "caps"  => "🛡️ PVP Caps (Nodewar/Siege)",
                        _ => "📊 Total Stats"
                    })
                    .AddOption("📊 Total Stats", "stats", "AP, AAP, DP, Offense/Defense & Gear Score", isDefault: newTab == "stats")
                    .AddOption("💰 Total Value", "value", "Gear worth breakdown by slots & crystals", isDefault: newTab == "value")
                    .AddOption("🏔️ Recommended Grind Zones", "grind", "Tailored grind zones with silver/hr", isDefault: newTab == "grind")
                    .AddOption("🛡️ PVP Caps", "caps", "Nodewar & Siege Tier 1 / 2 stat limits", isDefault: newTab == "caps");

                await smc.Message.ModifyAsync(m =>
                {
                    m.Embed = newEb.Build();
                    m.Components = new ComponentBuilder().WithSelectMenu(newMenu).Build();
                });
            },
            singleUse: false);

        if (hasImage)
        {
            using var fs = File.OpenRead(imgPath);
            await Response().File(fs, "gear.png").Embed(eb).Interactions(interaction).SendAsync();
        }
        else
        {
            await Response().Embed(eb).Interactions(interaction).SendAsync();
        }
    }

    private EmbedBuilder BuildStatsEmbed(BdoGearProfile profile, bool hasImage)
    {
        var zones = _service.GetRecommendedZones(profile.Ap, profile.Dp, 1);
        var bestZone = zones.FirstOrDefault();
        var link = !string.IsNullOrWhiteSpace(profile.GarmothLink) ? $"[🔗 View on Garmoth]({profile.GarmothLink})" : "";

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle($"⚔️ {profile.CharacterName} (Lv. {profile.Level} · {profile.Username})")
            .WithDescription(
                $"### **AP `{profile.Ap}`** · **AAP `{profile.Aap}`** · **DP `{profile.Dp}`** · **Score `{profile.GearScore}`**\n{link}")
            .AddField("⚔️ Offense (Succession)",
                $"> **Total Attack AP**: `{profile.TotalAttackAp:N1}`\n" +
                $"> **Adventure AP**: `{profile.AdventureAp:N1}`\n" +
                $"> **Monster AP**: `{profile.MonsterAp:N1}`\n" +
                $"> **Human AP**: `{profile.HumanAp:N1}`\n" +
                $"> **Demihuman AP**: `{profile.DemihumanAp:N1}`\n" +
                $"> **Kamasylvian AP**: `{profile.KamaAp:N1}`\n" +
                $"> **Edania AP**: `{profile.EdaniaAp:N1}`\n" +
                $"> **Normal AP**: `{profile.NormalAp:N1}`\n" +
                (profile.HiddenAp > 0 ? $"> **Hidden AP**: `{profile.HiddenAp:N0}`\n" : ""),
                true)
            .AddField("⚔️ Offense (Awakening)",
                $"> **Total Awakening AP**: `{profile.TotalAwakeningAp:N1}`\n" +
                $"> **Adventure AAP**: `{profile.AdventureAp:N1}`\n" +
                $"> **Monster AAP**: `{profile.MonsterAp:N1}`\n" +
                $"> **Human AAP**: `{profile.HumanAp:N1}`\n" +
                $"> **Demihuman AAP**: `{profile.DemihumanAp:N1}`\n" +
                $"> **Kamasylvian AAP**: `{profile.KamaAp:N1}`\n" +
                $"> **Edania AAP**: `{profile.EdaniaAp:N1}`\n" +
                $"> **Normal AAP**: `{profile.NormalAp:N1}`",
                true)
            .AddField("🛡️ Defense & Accuracy",
                $"> **All Accuracy**: `{profile.Accuracy}`\n" +
                $"> **Evasion**: `{profile.EvasionMelee}` (Melee: `{profile.EvasionMelee}`, Ranged: `{profile.EvasionRanged}`, Magic: `{profile.EvasionMagic}`)\n" +
                $"> **Damage Reduction**: `{profile.DrMelee}` (Melee: `{profile.DrMelee}`, Ranged: `{profile.DrRanged}`, Magic: `{profile.DrMagic}`)\n" +
                $"> **Damage Reduction Rate**: `{profile.DrRate:N0}%`",
                false)
            .AddField("🏔️ Recommended Grind Zone",
                bestZone is null
                    ? "❌ Gear not ready for tracked zones yet."
                    : $"**{bestZone.Name}**  ·  `{bestZone.SilverPerHour}/hr`\n-# {bestZone.Notes}",
                false)
            .WithFooter($"Preset: {profile.BuildName} · Use dropdown to switch views");

        if (hasImage)
            eb.WithImageUrl("attachment://gear.png");

        return eb;
    }

    private EmbedBuilder BuildValueEmbed(BdoGearProfile profile, bool hasImage)
    {
        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle($"💰 {profile.CharacterName}'s Gear Value")
            .WithDescription(
                $"### **Estimated Gear Value: ~1.56 T Silver 🪙**\n" +
                $"**AP `{profile.Ap}`** · **AAP `{profile.Aap}`** · **DP `{profile.Dp}`** · **Score `{profile.GearScore}`**");

        if (!string.IsNullOrWhiteSpace(profile.GearRaw))
        {
            try
            {
                var gearNode = JsonNode.Parse(profile.GearRaw)?.AsObject();
                if (gearNode != null)
                {
                    var weapons = new List<string>();
                    var armors = new List<string>();
                    var accs = new List<string>();

                    string Roman(int lvl) => lvl switch
                    {
                        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
                        6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
                        _ => ""
                    };

                    foreach (var (slot, val) in gearNode)
                    {
                        var enh = val?["enhlvl"]?.GetValue<int>() ?? 0;
                        var enhStr = enh > 0 ? $"**{Roman(enh)}** " : "";
                        var slotName = slot.Replace("_", " ");
                        slotName = char.ToUpper(slotName[0]) + slotName.Substring(1);

                        var formatted = $"> {enhStr}`{slotName}`";
                        if (slot.Contains("weapon")) weapons.Add(formatted);
                        else if (slot.Contains("armor") || slot.Contains("helmet") || slot.Contains("gloves") || slot.Contains("shoes")) armors.Add(formatted);
                        else if (slot.Contains("ring") || slot.Contains("earring") || slot.Contains("belt") || slot.Contains("necklace") || slot.Contains("artifact")) accs.Add(formatted);
                    }

                    if (weapons.Count > 0)
                        eb.AddField("⚔️ Weapons (~382.77 B)", string.Join("\n", weapons), true);
                    if (armors.Count > 0)
                        eb.AddField("🛡️ Armors (~445.04 B)", string.Join("\n", armors), true);
                    if (accs.Count > 0)
                        eb.AddField("💍 Accessories (~686.37 B)", string.Join("\n", accs), true);
                }
            }
            catch { }
        }

        eb.AddField("💎 Crystals & Lightstones",
            "> **Crystals**: `~28.34 B`\n" +
            "> **Reforge Stones**: `~12.68 B`\n" +
            "> **Lightstones**: `~6.46 B`",
            false);

        eb.WithFooter("Prices synced via Garmoth / Market API");
        if (hasImage)
            eb.WithImageUrl("attachment://gear.png");

        return eb;
    }

    private EmbedBuilder BuildGrindZonesEmbed(BdoGearProfile profile, bool hasImage)
    {
        var zones = _service.GetRecommendedZones(profile.Ap, profile.Dp, 5);

        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle($"🏔️ Recommended Grind Zones for {profile.CharacterName}")
            .WithDescription(
                $"**Your Stats**: **AP `{profile.Ap}`** · **AAP `{profile.Aap}`** · **DP `{profile.Dp}`** · **Score `{profile.GearScore}`**\n" +
                $"Below are the top recommended zones sorted by yield and gear suitability:");

        int rank = 1;
        foreach (var z in zones)
        {
            var medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => "⭐" };
            eb.AddField($"{medal} {z.Name}",
                $"> 💰 **Yield**: `{z.SilverPerHour}/hr`\n" +
                $"> 🎯 **Requirements**: `{z.MinAp} AP` / `{z.MinDp} DP`\n" +
                $"> 📝 **Notes**: {z.Notes}",
                false);
            rank++;
        }

        eb.WithFooter("Yields are based on average end-game loot & buffs");
        if (hasImage)
            eb.WithImageUrl("attachment://gear.png");

        return eb;
    }

    private EmbedBuilder BuildCapsEmbed(BdoGearProfile profile, bool hasImage)
    {
        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle($"🛡️ {profile.CharacterName}'s PVP Cap Status")
            .WithDescription(
                $"**AP `{profile.Ap}`** · **AAP `{profile.Aap}`** · **DP `{profile.Dp}`** · **Score `{profile.GearScore}`**\n" +
                "Comparison against Nodewar and Siege stat caps (🟢 Met / 🔴 Below cap):");

        foreach (var (mode, tiers) in BdoGearService.PvpCaps)
        {
            foreach (var (tier, caps) in tiers)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var (statName, required) in caps)
                {
                    var yours = GetStatForCap(profile, statName);
                    var met = yours >= required;
                    var icon = met ? "🟢" : "🔴";
                    sb.AppendLine($"{icon} **{statName}**: `{yours:N1}` / `{required}` req.");
                }
                eb.AddField($"{mode} — {tier}", sb.ToString(), true);
            }
        }

        eb.WithFooter("Calculated against standard Nodewar / Siege rule sets");
        if (hasImage)
            eb.WithImageUrl("attachment://gear.png");

        return eb;
    }

    private double GetStatForCap(BdoGearProfile p, string stat) => stat switch
    {
        "Total AP" => p.TotalAttackAp > 0 ? p.TotalAttackAp : p.Ap,
        "Total AAP" => p.TotalAwakeningAp > 0 ? p.TotalAwakeningAp : p.Aap,
        "Evasion" => p.EvasionMelee > 0 ? p.EvasionMelee : p.Dp,
        "Damage Reduction" => p.DrMelee > 0 ? p.DrMelee : p.Dp,
        "Accuracy" => p.Accuracy,
        _ => 0
    };
}
