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

        var embed = BuildCharacterEmbed(character);
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
            .WithColor(GetRarityColor(character.Rarity))
            .WithImageUrl(character.Images?.Full ?? character.Images?.Card ?? "")
            .WithFooter("Data from NIKKE.gg");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    private EmbedBuilder BuildCharacterEmbed(NikkeCharacter c)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{c.Rarity} {c.Name}")
            .WithColor(GetRarityColor(c.Rarity))
            .WithThumbnailUrl(c.Images?.Icon ?? "");

        // Backstory
        if (!string.IsNullOrEmpty(c.Backstory))
        {
            var desc = c.Backstory.Length > 500 ? c.Backstory[..500] + "..." : c.Backstory;
            embed.WithDescription(desc);
        }

        // Details field
        var details = new List<string>();
        if (!string.IsNullOrEmpty(c.Class))
            details.Add($"**Class:** {c.Class}");
        if (!string.IsNullOrEmpty(c.Weapon))
            details.Add($"**Weapon:** {c.Weapon}");
        if (!string.IsNullOrEmpty(c.Manufacturer))
            details.Add($"**Manufacturer:** {c.Manufacturer}");
        if (!string.IsNullOrEmpty(c.Element))
            details.Add($"**Element:** {c.Element}");
        if (!string.IsNullOrEmpty(c.BurstType))
            details.Add($"**Burst:** Type {c.BurstType}");
        if (!string.IsNullOrEmpty(c.Squad))
            details.Add($"**Squad:** {c.Squad}");
        if (!string.IsNullOrEmpty(c.BurstGeneration))
            details.Add($"**Burst Gen:** {c.BurstGeneration}");

        if (details.Count > 0)
            embed.AddField("Details", string.Join("\n", details), inline: true);

        // Tierlist field
        if (c.Tierlist != null && (c.Tierlist.Combined != null || c.Tierlist.Story != null || c.Tierlist.Boss != null || c.Tierlist.PvP != null))
        {
            var tierLines = new List<string>();
            if (!string.IsNullOrEmpty(c.Tierlist.Combined))
                tierLines.Add($"**Combined:** {c.Tierlist.Combined}");
            if (!string.IsNullOrEmpty(c.Tierlist.Story))
                tierLines.Add($"**Story:** {c.Tierlist.Story}");
            if (!string.IsNullOrEmpty(c.Tierlist.Boss))
                tierLines.Add($"**Boss:** {c.Tierlist.Boss}");
            if (!string.IsNullOrEmpty(c.Tierlist.PvP))
                tierLines.Add($"**PvP:** {c.Tierlist.PvP}");

            if (tierLines.Count > 0)
                embed.AddField("Tierlist (NIKKE.gg)", string.Join("\n", tierLines), inline: true);
        }

        // Normal Attack
        if (c.Skills?.Normal != null)
        {
            var normalLines = new List<string>();
            if (!string.IsNullOrEmpty(c.Skills.Normal.Mode))
                normalLines.Add($"**Mode:** {c.Skills.Normal.Mode}");
            if (c.Skills.Normal.Ammo.HasValue)
                normalLines.Add($"**Ammo:** {c.Skills.Normal.Ammo.Value}");
            if (!string.IsNullOrEmpty(c.Skills.Normal.ReloadTime))
                normalLines.Add($"**Reload:** {c.Skills.Normal.ReloadTime}");
            if (!string.IsNullOrEmpty(c.DamagePercent))
                normalLines.Add($"**Damage:** {c.DamagePercent} ATK");
            if (!string.IsNullOrEmpty(c.ChargeTime) && c.ChargeTime != "0s")
                normalLines.Add($"**Charge:** {c.ChargeTime} / {c.ChargeDamage}");
            if (!string.IsNullOrEmpty(c.Skills.Normal.Description?.FirstOrDefault()))
                normalLines.Add($"\n{FormatSkillLines(c.Skills.Normal.Description)}");

            if (normalLines.Count > 0)
            {
                var text = string.Join("\n", normalLines);
                if (text.Length > 1024) text = text[..1021] + "...";
                embed.AddField($"🎯 Normal Attack", text);
            }
        }

        // Skill 1
        if (c.Skills?.Skill1 != null)
        {
            var skillText = FormatSkill(c.Skills.Skill1);
            if (!string.IsNullOrEmpty(skillText))
            {
                if (skillText.Length > 1024) skillText = skillText[..1021] + "...";
                embed.AddField($"🗡️ {c.Skills.Skill1.Name} [{c.Skills.Skill1.Type}]", skillText);
            }
        }

        // Skill 2
        if (c.Skills?.Skill2 != null)
        {
            var skillText = FormatSkill(c.Skills.Skill2);
            if (!string.IsNullOrEmpty(skillText))
            {
                if (skillText.Length > 1024) skillText = skillText[..1021] + "...";
                embed.AddField($"🛡️ {c.Skills.Skill2.Name} [{c.Skills.Skill2.Type}]", skillText);
            }
        }

        // Burst
        if (c.Skills?.Burst != null)
        {
            var skillText = FormatSkill(c.Skills.Burst);
            if (!string.IsNullOrEmpty(skillText))
            {
                if (skillText.Length > 1024) skillText = skillText[..1021] + "...";
                var cd = string.IsNullOrEmpty(c.Skills.Burst.Cooldown) ? "" : $" [{c.Skills.Burst.Cooldown}]";
                embed.AddField($"💥 {c.Skills.Burst.Name} [{c.Skills.Burst.Type}]{cd}", skillText);
            }
        }

        // Skill Priority
        if (c.SkillPriority != null && (c.SkillPriority.Budget != null || c.SkillPriority.Recommended != null))
        {
            var prioLines = new List<string>();
            if (!string.IsNullOrEmpty(c.SkillPriority.Budget))
                prioLines.Add($"**Budget:** {c.SkillPriority.Budget}");
            if (!string.IsNullOrEmpty(c.SkillPriority.Recommended))
                prioLines.Add($"**Recommended:** {c.SkillPriority.Recommended}");
            if (!string.IsNullOrEmpty(c.SkillPriority.Order))
                prioLines.Add($"**Order:** {c.SkillPriority.Order}");

            if (prioLines.Count > 0)
                embed.AddField("Skill Priority", string.Join("\n", prioLines), inline: true);
        }

        // Cubes
        if (c.Cubes != null && (c.Cubes.Main != null || c.Cubes.Alternative != null))
        {
            var cubeLines = new List<string>();
            if (!string.IsNullOrEmpty(c.Cubes.Main))
                cubeLines.Add($"**Main:** {c.Cubes.Main}");
            if (!string.IsNullOrEmpty(c.Cubes.Alternative))
                cubeLines.Add($"**Alt:** {c.Cubes.Alternative}");

            if (cubeLines.Count > 0)
                embed.AddField("Cube Recommendations", string.Join("\n", cubeLines), inline: true);
        }

        // Voice Actors
        var vaLines = new List<string>();
        if (!string.IsNullOrEmpty(c.VoiceActors?.En))
            vaLines.Add($"🇺🇸 {c.VoiceActors.En}");
        if (!string.IsNullOrEmpty(c.VoiceActors?.Jp))
            vaLines.Add($"🇯🇵 {c.VoiceActors.Jp}");
        if (!string.IsNullOrEmpty(c.VoiceActors?.Kr))
            vaLines.Add($"🇰🇷 {c.VoiceActors.Kr}");
        if (vaLines.Count > 0)
            embed.AddField("Voice Actors", string.Join("\n", vaLines), inline: true);

        // Full body image at bottom
        if (!string.IsNullOrEmpty(c.Images?.Full))
            embed.WithImageUrl(c.Images.Full);

        embed.WithFooter("Data from NIKKE.gg • Skills shown at Level 10");

        return embed;
    }

    private static string FormatSkill(NikkeSkill skill)
    {
        if (skill == null) return null;

        var desc = skill.BaseDescription ?? skill.MaxDescription ?? skill.Description;
        if (desc == null || desc.Count == 0)
            return null;

        return FormatSkillLines(desc);
    }

    private static string FormatSkillLines(List<string> lines)
    {
        if (lines == null || lines.Count == 0)
            return "No description available.";

        var formatted = lines.Select(l => l.StartsWith("■") ? $"▸ {l[1..].Trim()}" : l).ToList();
        return string.Join("\n", formatted);
    }

    private static Color GetRarityColor(string rarity)
    {
        return rarity?.ToUpperInvariant() switch
        {
            "R" => new Color(0, 144, 255),       // Blue
            "SR" => new Color(191, 0, 254),      // Purple
            "SSR" => new Color(255, 192, 0),    // Gold
            _ => new Color(185, 35, 35)
        };
    }
}
