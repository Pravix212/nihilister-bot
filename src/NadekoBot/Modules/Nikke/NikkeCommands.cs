#nullable disable
using Discord;
using NadekoBot.Modules.Nikke.Services;

namespace NadekoBot.Modules.Nikke;

public partial class NikkeCommands : NadekoModule
{
    private readonly NikkeService _svc;

    public NikkeCommands(NikkeService svc)
    {
        _svc = svc;
    }

    [Cmd]
    public async Task Nikke([Leftover] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await Response().Error("Please provide a character name. Example: `.nikke nihilister`").SendAsync();
            return;
        }

        await Response().Pending($"Searching for **{name}**...").SendAsync();

        var character = await _svc.GetCharacterAsync(name);
        if (character == null)
        {
            await Response().Error($"Could not find a NIKKE character named '{name}'.").SendAsync();
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle(character.Name)
            .WithColor(GetElementColor(character.Element))
            .WithThumbnailUrl(character.Images?.Icon ?? "");

        // Class, rarity, element, manufacturer, burst
        var details = new List<string>();
        if (!string.IsNullOrEmpty(character.Rarity))
            details.Add($"**Rarity:** {character.Rarity}");
        if (!string.IsNullOrEmpty(character.Class))
            details.Add($"**Class:** {character.Class}");
        if (!string.IsNullOrEmpty(character.Element))
            details.Add($"**Element:** {character.Element}");
        if (!string.IsNullOrEmpty(character.BurstType))
            details.Add($"**Burst:** {character.BurstType}");
        if (!string.IsNullOrEmpty(character.Manufacturer))
            details.Add($"**Manufacturer:** {character.Manufacturer}");
        if (!string.IsNullOrEmpty(character.Squad))
            details.Add($"**Squad:** {character.Squad}");
        if (!string.IsNullOrEmpty(character.Weapon))
            details.Add($"**Weapon:** {character.Weapon}{(string.IsNullOrEmpty(character.WeaponName) ? "" : $" ({character.WeaponName})")}");

        embed.AddField("Details", string.Join("\n", details), inline: true);

        // Stats
        if (character.Stats != null && (character.Stats.Hp.HasValue || character.Stats.Atk.HasValue || character.Stats.Def.HasValue))
        {
            var statsLines = new List<string>();
            if (character.Stats.Hp.HasValue)
                statsLines.Add($"**HP:** {character.Stats.Hp.Value:N0}");
            if (character.Stats.Atk.HasValue)
                statsLines.Add($"**ATK:** {character.Stats.Atk.Value:N0}");
            if (character.Stats.Def.HasValue)
                statsLines.Add($"**DEF:** {character.Stats.Def.Value:N0}");
            embed.AddField("Stats", string.Join("\n", statsLines), inline: true);
        }

        // Voice actors
        var vaLines = new List<string>();
        if (!string.IsNullOrEmpty(character.VoiceActors?.En))
            vaLines.Add($"🇺🇸 {character.VoiceActors.En}");
        if (!string.IsNullOrEmpty(character.VoiceActors?.Jp))
            vaLines.Add($"🇯🇵 {character.VoiceActors.Jp}");
        if (!string.IsNullOrEmpty(character.VoiceActors?.Kr))
            vaLines.Add($"🇰🇷 {character.VoiceActors.Kr}");
        if (vaLines.Count > 0)
            embed.AddField("Voice Actors", string.Join("\n", vaLines), inline: true);

        // Skills
        if (character.Skills != null)
        {
            var skillLines = new List<string>();
            
            if (character.Skills.Normal != null)
            {
                var desc = FormatSkillDescription(character.Skills.Normal.BaseDescription ?? character.Skills.Normal.Description);
                skillLines.Add($"**{character.Skills.Normal.Name}** — {desc}");
            }
            if (character.Skills.Skill1 != null)
            {
                var desc = FormatSkillDescription(character.Skills.Skill1.BaseDescription);
                var cd = string.IsNullOrEmpty(character.Skills.Skill1.Cooldown) ? "" : $" ({character.Skills.Skill1.Cooldown})";
                skillLines.Add($"**{character.Skills.Skill1.Name}**{cd} — {desc}");
            }
            if (character.Skills.Skill2 != null)
            {
                var desc = FormatSkillDescription(character.Skills.Skill2.BaseDescription);
                var cd = string.IsNullOrEmpty(character.Skills.Skill2.Cooldown) ? "" : $" ({character.Skills.Skill2.Cooldown})";
                skillLines.Add($"**{character.Skills.Skill2.Name}**{cd} — {desc}");
            }
            if (character.Skills.Burst != null)
            {
                var desc = FormatSkillDescription(character.Skills.Burst.BaseDescription);
                var cd = string.IsNullOrEmpty(character.Skills.Burst.Cooldown) ? "" : $" ({character.Skills.Burst.Cooldown})";
                skillLines.Add($"**{character.Skills.Burst.Name}**{cd} — {desc}");
            }

            if (skillLines.Count > 0)
            {
                var skillsText = string.Join("\n\n", skillLines);
                if (skillsText.Length > 1000)
                    skillsText = skillsText[..1000] + "\n\n... (truncated)";
                embed.AddField("Skills", skillsText);
            }
        }

        // Backstory (truncated if too long)
        if (!string.IsNullOrEmpty(character.Backstory))
        {
            var backstory = character.Backstory.Length > 500 ? character.Backstory[..500] + "..." : character.Backstory;
            embed.AddField("Backstory", backstory);
        }

        // Card image
        if (!string.IsNullOrEmpty(character.Images?.Card))
            embed.WithImageUrl(character.Images.Card);

        embed.WithFooter("Data from NikkeAPI / prydwen.gg");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Cmd]
    public async Task NikkeArt([Leftover] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await Response().Error("Please provide a character name. Example: `.nikkeart nihilister`").SendAsync();
            return;
        }

        await Response().Pending($"Fetching artwork for **{name}**...").SendAsync();

        var character = await _svc.GetCharacterAsync(name);
        if (character == null)
        {
            await Response().Error($"Could not find a NIKKE character named '{name}'.").SendAsync();
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{character.Name} — Full Artwork")
            .WithColor(GetElementColor(character.Element))
            .WithImageUrl(character.Images?.Full ?? character.Images?.Card ?? "")
            .WithFooter("Data from NikkeAPI / prydwen.gg");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    private static string FormatSkillDescription(List<string> description)
    {
        if (description == null || description.Count == 0)
            return "No description available.";
        return string.Join(" ", description);
    }

    private static Color GetElementColor(string element)
    {
        return element?.ToLowerInvariant() switch
        {
            "fire" => new Color(220, 60, 40),
            "water" => new Color(40, 120, 220),
            "wind" => new Color(40, 180, 80),
            "electric" => new Color(220, 200, 40),
            "iron" => new Color(120, 120, 120),
            _ => new Color(185, 35, 35)
        };
    }
}
