using NadekoBot.Modules.BlackDesertOnline.Models;

namespace NadekoBot.Modules.BlackDesertOnline;

public partial class Bdo
{
    private static readonly SemaphoreSlim _registrationLock = new(1, 1);
    private static readonly HashSet<ulong> _registrationInProgress = new();

    [Cmd]
    public async Task BdoGearRegister()
    {
        var userId = ctx.User.Id;

        if (!_registrationLock.Wait(0))
        {
            await Response().Error("Please wait before starting another registration.").SendAsync();
            return;
        }

        try
        {
            if (_registrationInProgress.Contains(userId))
            {
                await Response().Error("You already have a registration in progress!").SendAsync();
                return;
            }
            _registrationInProgress.Add(userId);

            var profile = new BdoGearProfile
            {
                UserId = userId,
                Username = ctx.User.Username,
                RegisteredAt = DateTime.UtcNow,
            };

            await Response()
                .Embed(CreateEmbed().WithOkColor()
                    .WithTitle("⚔️ BDO Gear Registration")
                    .WithDescription("""
                        I'll ask you a series of questions. Type each stat value and send it.
                        Type `skip` to leave a stat as 0, or `cancel` to abort.

                        **Starting with your core stats...**
                        """)
                    .WithFooter("You have 60 seconds to answer each question."))
                .SendAsync();

            var cancelled = false;

            async Task<int?> AskStat(string statName, string description = "")
            {
                if (cancelled) return null;
                await ctx.Channel.SendMessageAsync(
                    embed: CreateEmbed().WithPendingColor()
                        .WithTitle($"📊 {statName}")
                        .WithDescription(string.IsNullOrEmpty(description)
                            ? $"Enter your **{statName}**:"
                            : description)
                        .WithFooter("Type the value, 'skip' for 0, or 'cancel' to abort")
                        .Build());

                var response = await GetUserInputWithTimeoutAsync(userId, ctx.Channel.Id, 60_000);

                if (response is null)
                {
                    await ctx.Channel.SendMessageAsync($"⏰ Timed out waiting for **{statName}**. Registration cancelled.");
                    cancelled = true;
                    return null;
                }

                if (response.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ctx.Channel.SendMessageAsync("❌ Registration cancelled.");
                    cancelled = true;
                    return null;
                }

                if (response.Equals("skip", StringComparison.OrdinalIgnoreCase)) return 0;

                if (int.TryParse(response.Trim(), out var val) && val >= 0)
                    return val;

                await ctx.Channel.SendMessageAsync($"⚠️ Invalid number for **{statName}**, treating as 0.");
                return 0;
            }

            // --- Core Stats ---
            var ap = await AskStat("AP (Attack Power)", "Your base **AP** (e.g. 367)");
            if (cancelled) return;
            profile.Ap = ap ?? 0;

            var aap = await AskStat("AAP (Awakening Attack Power)", "Your base **AAP** (e.g. 365)");
            if (cancelled) return;
            profile.Aap = aap ?? 0;

            var dp = await AskStat("DP (Defense Power)", "Your base **DP** (e.g. 445)");
            if (cancelled) return;
            profile.Dp = dp ?? 0;

            profile.GearScore = (profile.Ap + profile.Aap) / 2 + profile.Dp;

            // --- Succession Stats ---
            await ctx.Channel.SendMessageAsync(
                embed: CreateEmbed().WithOkColor()
                    .WithTitle("📊 Succession Stats")
                    .WithDescription("Now enter your **Total Stats** values. You can find these in Garmoth under **Stats > Offense (Succession)**.")
                    .Build());

            var totalAp = await AskStat("Total Attack AP");
            if (cancelled) return;
            profile.TotalAttackAp = totalAp ?? 0;

            var monsterAp = await AskStat("Monster AP");
            if (cancelled) return;
            profile.MonsterAp = monsterAp ?? 0;

            var humanAp = await AskStat("Human AP");
            if (cancelled) return;
            profile.HumanAp = humanAp ?? 0;

            var demihumanAp = await AskStat("Demihuman AP");
            if (cancelled) return;
            profile.DemihumanAp = demihumanAp ?? 0;

            var hiddenAp = await AskStat("Hidden AP");
            if (cancelled) return;
            profile.HiddenAp = hiddenAp ?? 0;

            // --- Awakening Stats ---
            await ctx.Channel.SendMessageAsync(
                embed: CreateEmbed().WithOkColor()
                    .WithTitle("📊 Awakening Stats")
                    .WithDescription("Now from **Offense (Awakening)**:")
                    .Build());

            var totalAap = await AskStat("Total Awakening AP");
            if (cancelled) return;
            profile.TotalAwakeningAp = totalAap ?? 0;

            var monsterAap = await AskStat("Monster AAP");
            if (cancelled) return;
            profile.MonsterAap = monsterAap ?? 0;

            var humanAap = await AskStat("Human AAP");
            if (cancelled) return;
            profile.HumanAap = humanAap ?? 0;

            var demihumanAap = await AskStat("Demihuman AAP");
            if (cancelled) return;
            profile.DemihumanAap = demihumanAap ?? 0;

            // --- Defense ---
            await ctx.Channel.SendMessageAsync(
                embed: CreateEmbed().WithOkColor()
                    .WithTitle("🛡️ Defense Stats")
                    .WithDescription("Almost done! Enter your **Defense** stats:")
                    .Build());

            var evasion = await AskStat("Evasion");
            if (cancelled) return;
            profile.Evasion = evasion ?? 0;

            var dr = await AskStat("Damage Reduction");
            if (cancelled) return;
            profile.DamageReduction = dr ?? 0;

            var accuracy = await AskStat("Accuracy");
            if (cancelled) return;
            profile.Accuracy = accuracy ?? 0;

            var maxHp = await AskStat("Max HP");
            if (cancelled) return;
            profile.MaxHp = maxHp ?? 0;

            // Optional garmoth link
            await ctx.Channel.SendMessageAsync(
                embed: CreateEmbed().WithPendingColor()
                    .WithTitle("🔗 Garmoth Link (Optional)")
                    .WithDescription("Type your Garmoth character link (e.g. `https://garmoth.com/character/pravv`) or type `skip`:")
                    .Build());
            var linkInput = await GetUserInputWithTimeoutAsync(userId, ctx.Channel.Id, 60_000);
            if (linkInput is not null
                && !linkInput.Equals("skip", StringComparison.OrdinalIgnoreCase)
                && !linkInput.Equals("cancel", StringComparison.OrdinalIgnoreCase)
                && linkInput.StartsWith("https://garmoth.com/"))
                profile.GarmothLink = linkInput.Trim();

            _service.SaveProfile(profile);

            var zone = _service.GetRecommendedZone(profile.Ap, profile.Dp);
            var eb = CreateEmbed().WithOkColor()
                .WithTitle($"✅ Gear Profile Saved — {ctx.User.Username}")
                .WithDescription(
                    $"**AP** `{profile.Ap}` · **AAP** `{profile.Aap}` · **DP** `{profile.Dp}` · **Score** `{profile.GearScore}`")
                .AddField("🏔️ Recommended Grind Zone",
                    zone is null
                        ? "Work on your gear first! You're not ready for any zone yet."
                        : $"**{zone.Name}** — {zone.SilverPerHour}/hr\n-# {zone.Notes}",
                    false)
                .WithFooter("Use .bdogear to view your profile");

            await Response().Embed(eb).SendAsync();
        }
        finally
        {
            _registrationInProgress.Remove(userId);
            _registrationLock.Release();
        }
    }

    [Cmd]
    public async Task BdoGear(IUser? user = null)
    {
        var target = user ?? ctx.User;
        var profile = _service.GetProfile(target.Id);

        if (profile is null)
        {
            var msg = target.Id == ctx.User.Id
                ? "You don't have a gear profile yet! Use `.bdogearregister` to create one."
                : $"**{target.Username}** doesn't have a gear profile yet.";
            await Response().Error(msg).SendAsync();
            return;
        }

        await ShowGearProfileAsync(profile, "stats");
    }

    [Cmd]
    [OwnerOnly]
    public async Task BdoGearDelete(IUser? user = null)
    {
        var target = user ?? ctx.User;
        _service.DeleteProfile(target.Id);
        await ctx.OkAsync();
    }

    private async Task ShowGearProfileAsync(BdoGearProfile profile, string tab)
    {
        var eb = BuildTabEmbed(profile, tab);

        var tabMenu = BuildTabMenu(profile.UserId, tab);
        var interaction = _inter.Create(
            ctx.User.Id,
            tabMenu,
            async (smc) =>
            {
                var newTab = smc.Data.Values.First();
                await smc.DeferAsync();
                var newEb = BuildTabEmbed(profile, newTab);
                var newMenu = BuildTabMenu(profile.UserId, newTab);
                await smc.Message.ModifyAsync(m =>
                {
                    m.Embed = newEb.Build();
                    m.Components = new ComponentBuilder().WithSelectMenu(newMenu).Build();
                });
            },
            singleUse: false);

        await Response().Embed(eb).Interactions(interaction).SendAsync();
    }

    private SelectMenuBuilder BuildTabMenu(ulong profileUserId, string currentTab)
        => new SelectMenuBuilder()
            .WithCustomId($"bdo_gear_tab:{profileUserId}")
            .WithPlaceholder(currentTab switch { "stats" => "📊 Stats", "caps" => "🛡️ PVP Caps", _ => "📊 Stats" })
            .AddOption("📊 Stats", "stats", "View gear stats & grind zone", isDefault: currentTab == "stats")
            .AddOption("🛡️ PVP Caps", "caps", "Nodewar & Siege cap comparison", isDefault: currentTab == "caps");

    private EmbedBuilder BuildTabEmbed(BdoGearProfile profile, string tab)
        => tab switch
        {
            "caps" => BuildCapsEmbed(profile),
            _ => BuildStatsEmbed(profile),
        };

    private EmbedBuilder BuildStatsEmbed(BdoGearProfile profile)
    {
        var zone = _service.GetRecommendedZone(profile.Ap, profile.Dp);
        var link = profile.GarmothLink is not null ? $"[View on Garmoth]({profile.GarmothLink})" : null;

        return CreateEmbed()
            .WithOkColor()
            .WithTitle($"⚔️ {profile.Username}'s Gear Profile")
            .WithDescription(
                $"**AP** `{profile.Ap}` · **AAP** `{profile.Aap}` · **DP** `{profile.Dp}` · **Score** `{profile.GearScore}`"
                + (link is not null ? $"\n{link}" : ""))
            .AddField("⚔️ Offense (Succession)",
                $"> Total AP: `{profile.TotalAttackAp}`\n" +
                $"> Monster AP: `{profile.MonsterAp}`\n" +
                $"> Human AP: `{profile.HumanAp}`\n" +
                $"> Demihuman AP: `{profile.DemihumanAp}`\n" +
                $"> Hidden AP: `{profile.HiddenAp}`",
                true)
            .AddField("⚔️ Offense (Awakening)",
                $"> Total AAP: `{profile.TotalAwakeningAp}`\n" +
                $"> Monster AAP: `{profile.MonsterAap}`\n" +
                $"> Human AAP: `{profile.HumanAap}`\n" +
                $"> Demihuman AAP: `{profile.DemihumanAap}`",
                true)
            .AddField("🛡️ Defense",
                $"> Evasion: `{profile.Evasion}`\n" +
                $"> Damage Reduction: `{profile.DamageReduction}`\n" +
                $"> Accuracy: `{profile.Accuracy}`\n" +
                $"> Max HP: `{profile.MaxHp:N0}`",
                false)
            .AddField("🏔️ Recommended Grind Zone",
                zone is null
                    ? "❌ Gear not ready for tracked zones yet."
                    : $"**{zone.Name}**  ·  {zone.SilverPerHour}/hr\n-# {zone.Notes}",
                false)
            .WithFooter($"Registered {profile.RegisteredAt:yyyy-MM-dd}");
    }

    private EmbedBuilder BuildCapsEmbed(BdoGearProfile profile)
    {
        var eb = CreateEmbed()
            .WithOkColor()
            .WithTitle($"🛡️ {profile.Username}'s PVP Cap Status")
            .WithDescription(
                $"**AP** `{profile.Ap}` · **AAP** `{profile.Aap}` · **DP** `{profile.Dp}` · **Score** `{profile.GearScore}`");

        foreach (var (mode, tiers) in BdoGearService.PvpCaps)
        {
            foreach (var (tier, caps) in tiers)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var (statName, required) in caps)
                {
                    var yours = GetStatForCap(profile, statName);
                    var icon = yours >= required ? "🟢" : "🔴";
                    sb.AppendLine($"{icon} **{statName}**: `{yours}` / `{required}` req.");
                }
                eb.AddField($"{mode} — {tier}", sb.ToString(), true);
            }
        }

        return eb;
    }

    private static int GetStatForCap(BdoGearProfile p, string stat) => stat switch
    {
        "Total AP" => p.TotalAttackAp > 0 ? p.TotalAttackAp : p.Ap,
        "Total AAP" => p.TotalAwakeningAp > 0 ? p.TotalAwakeningAp : p.Aap,
        "Evasion" => p.Evasion,
        "Damage Reduction" => p.DamageReduction,
        "Accuracy" => p.Accuracy,
        "Max HP" => p.MaxHp,
        _ => 0
    };
}