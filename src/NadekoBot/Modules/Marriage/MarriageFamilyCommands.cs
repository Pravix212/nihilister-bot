using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Marriage;

public partial class Marriage
{
    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Adopt([Leftover] IUser? target = null)
    {
        await CheckAndNotifyExpiredAsync();

        if (target == null || target.Id == ctx.User.Id)
        {
            await Response().Error("Mention someone to adopt! Example: .adopt @user").SendAsync();
            return;
        }

        if (target.IsBot)
        {
            await Response().Error("You cannot adopt a bot!").SendAsync();
            return;
        }

        if (await _svc.IsAdoptedAsync(target.Id))
        {
            await Response().Error($"{target.Mention} is already adopted!").SendAsync();
            return;
        }

        // Check if I already proposed to adopt the target
        var proposerToTarget = await _svc.GetAdoptionProposalAsync(target.Id);
        if (proposerToTarget == ctx.User.Id)
        {
            await Response().Error("You already proposed to adopt them! Wait for them to accept. (Expires in 1 minute)").SendAsync();
            return;
        }

        if (proposerToTarget != null)
        {
            await Response().Error($"{target.Mention} already has a pending adoption proposal from someone else!").SendAsync();
            return;
        }

        await _svc.AdoptAsync(ctx.User.Id, target.Id);
        MarriageService.ProposalChannels.Set(ctx.User.Id, target.Id, "adoption", ctx.Channel.Id, ctx.Guild.Id);
        ScheduleExpirationTimer(ctx.User.Id, target.Id, "adoption");

        await Response().Confirm($"{ctx.User.Mention} wants to adopt {target.Mention}!\n\n{target.Mention}, type `.accept` to accept! (Expires in 1 minute)").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Disown([Leftover] IUser? target = null)
    {
        await CheckAndNotifyExpiredAsync();

        if (target == null)
        {
            await Response().Error("Mention someone to disown! Example: .disown @user").SendAsync();
            return;
        }

        var children = await _svc.GetChildrenAsync(ctx.User.Id);
        if (!children.Contains(target.Id))
        {
            await Response().Error($"{target.Mention} is not your child!").SendAsync();
            return;
        }

        var success = await _svc.DisownAsync(ctx.User.Id, target.Id);
        if (success)
            await Response().Confirm($"You have disowned {target.Mention}. They are no longer your child. 💔").SendAsync();
        else
            await Response().Error("Something went wrong. Could not disown.").SendAsync();
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Family([Leftover] IUser? target = null)
    {
        await CheckAndNotifyExpiredAsync();

        var user = target ?? ctx.User;
        var spouseId = await _svc.GetSpouseAsync(user.Id);
        var adoption = await _svc.GetAdoptionAsync(user.Id);
        var children = await _svc.GetChildrenAsync(user.Id);
        var siblings = await _svc.GetSiblingsAsync(user.Id);

        var spouse = spouseId != null ? await ctx.Guild.GetUserAsync(spouseId.Value) : null;
        var parent1 = adoption != null && adoption.Parent1Id != 0 ? await ctx.Guild.GetUserAsync(adoption.Parent1Id) : null;
        var parent2 = adoption != null && adoption.Parent2Id != 0 ? await ctx.Guild.GetUserAsync(adoption.Parent2Id) : null;

        var childrenUsers = new List<IGuildUser>();
        foreach (var childId in children)
        {
            try { var c = await ctx.Guild.GetUserAsync(childId); if (c != null) childrenUsers.Add(c); }
            catch { }
        }

        var siblingUsers = new List<IGuildUser>();
        foreach (var sibId in siblings)
        {
            try { var s = await ctx.Guild.GetUserAsync(sibId); if (s != null) siblingUsers.Add(s); }
            catch { }
        }

        var embed = new EmbedBuilder()
            .WithTitle("Family Tree")
            .WithColor(Color.Teal)
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

        if (spouse != null)
        {
            var marriage = await _svc.GetMarriageAsync(user.Id);
            var duration = marriage != null ? FormatDuration(DateTime.UtcNow - marriage.MarriedAt) : "unknown";
            embed.AddField("Spouse", $"{spouse.Mention}\nMarried: {duration}", true);
        }

        if (parent1 != null || parent2 != null)
        {
            var parents = new List<string>();
            if (parent1 != null) parents.Add(parent1.Mention);
            if (parent2 != null) parents.Add(parent2.Mention);
            embed.AddField("Parents", string.Join(" + ", parents), true);
        }

        if (childrenUsers.Any())
            embed.AddField($"Children ({childrenUsers.Count})", string.Join("\n", childrenUsers.Select(c => c.Mention)), true);

        if (siblingUsers.Any())
            embed.AddField($"Siblings ({siblingUsers.Count})", string.Join("\n", siblingUsers.Select(s => s.Mention)), true);

        if (spouse == null && parent1 == null && parent2 == null && !childrenUsers.Any() && !siblingUsers.Any())
            embed.WithDescription("This user has no family yet! Use `.marry` and `.adopt` to build one.");

        await ctx.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    public async Task Familytree([Leftover] IUser? target = null)
    {
        await CheckAndNotifyExpiredAsync();

        var user = target ?? ctx.User;

        var spouseId = await _svc.GetSpouseAsync(user.Id);
        var adoption = await _svc.GetAdoptionAsync(user.Id);
        var children = await _svc.GetChildrenAsync(user.Id);

        var allUserIds = new HashSet<ulong> { user.Id };
        if (spouseId.HasValue) allUserIds.Add(spouseId.Value);
        if (adoption != null && adoption.Parent1Id != 0) allUserIds.Add(adoption.Parent1Id);
        if (adoption != null && adoption.Parent2Id != 0) allUserIds.Add(adoption.Parent2Id);
        foreach (var childId in children) allUserIds.Add(childId);

        var userNames = new Dictionary<ulong, string>();
        foreach (var uid in allUserIds)
        {
            try
            {
                var u = await ctx.Guild.GetUserAsync(uid);
                userNames[uid] = u?.Username ?? "Unknown";
            }
            catch { userNames[uid] = "Unknown"; }
        }

        if (allUserIds.Count == 1)
        {
            await Response().Error("You have no family to display! Use `.marry` and `.adopt` to build a family tree first.").SendAsync();
            return;
        }

        // Download avatars for the family tree
        var avatarPaths = new Dictionary<ulong, string>();
        var avatarDir = Path.Combine(Path.GetTempPath(), $"avatars_{user.Id}");
        Directory.CreateDirectory(avatarDir);

        using var httpClient = new HttpClient();
        foreach (var uid in allUserIds)
        {
            try
            {
                string? avatarUrl = null;
                try
                {
                    var guildUser = await ctx.Guild.GetUserAsync(uid);
                    avatarUrl = guildUser?.GetAvatarUrl(ImageFormat.Png, 512) ?? guildUser?.GetDefaultAvatarUrl();
                }
                catch
                {
                    var discordUser = await ctx.Client.GetUserAsync(uid);
                    avatarUrl = discordUser?.GetAvatarUrl(ImageFormat.Png, 512) ?? discordUser?.GetDefaultAvatarUrl();
                }

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    var avatarPath = Path.Combine(avatarDir, $"{uid}.png");
                    var avatarBytes = await httpClient.GetByteArrayAsync(avatarUrl);
                    await File.WriteAllBytesAsync(avatarPath, avatarBytes);
                    avatarPaths[uid] = avatarPath;
                }
            }
            catch { /* ignore avatar download failures */ }
        }

        // Check if ImageMagick is available and convert avatars to circles
        bool hasImageMagick = false;
        try
        {
            var psiCheck = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "convert",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var procCheck = Process.Start(psiCheck);
            if (procCheck != null)
            {
                await procCheck.WaitForExitAsync();
                hasImageMagick = procCheck.ExitCode == 0;
            }
        }
        catch { }

        if (hasImageMagick)
        {
            foreach (var kvp in avatarPaths.ToList())
            {
                try
                {
                    var circularPath = Path.Combine(avatarDir, $"{kvp.Key}_circle.png");
                    var psi = new ProcessStartInfo
                    {
                        FileName = "convert",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    // Step 1: square-crop to 256x256 centered
                    psi.ArgumentList.Add(kvp.Value);
                    psi.ArgumentList.Add("-resize");
                    psi.ArgumentList.Add("256x256^");
                    psi.ArgumentList.Add("-gravity");
                    psi.ArgumentList.Add("center");
                    psi.ArgumentList.Add("-extent");
                    psi.ArgumentList.Add("256x256");
                    // Step 2: apply circular alpha mask
                    psi.ArgumentList.Add("-alpha");
                    psi.ArgumentList.Add("set");
                    psi.ArgumentList.Add("(");
                    psi.ArgumentList.Add("+clone");
                    psi.ArgumentList.Add("-fill");
                    psi.ArgumentList.Add("black");
                    psi.ArgumentList.Add("-colorize");
                    psi.ArgumentList.Add("100%");
                    psi.ArgumentList.Add("-fill");
                    psi.ArgumentList.Add("white");
                    psi.ArgumentList.Add("-draw");
                    psi.ArgumentList.Add("circle 128,128 128,1");
                    psi.ArgumentList.Add(")");
                    psi.ArgumentList.Add("-compose");
                    psi.ArgumentList.Add("copyopacity");
                    psi.ArgumentList.Add("-composite");
                    psi.ArgumentList.Add(circularPath);
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                        if (proc.ExitCode == 0)
                        {
                            avatarPaths[kvp.Key] = circularPath;
                        }
                    }
                }
                catch { }
            }
        }

        var dot = GenerateFamilyTreeDot(user.Id, spouseId, adoption, children, userNames, avatarPaths, avatarDir);
        var tempDir = Path.GetTempPath();
        var dotFile = Path.Combine(tempDir, $"familytree_{user.Id}.dot");
        var pngFile = Path.Combine(tempDir, $"familytree_{user.Id}.png");

        try
        {
            await File.WriteAllTextAsync(dotFile, dot);

            var psi = new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = $"-Tpng \"{dotFile}\" -o \"{pngFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                await Response().Error("Failed to start Graphviz. Please contact the bot owner.").SendAsync();
                return;
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                await Response().Error($"Graphviz rendering failed: {error}").SendAsync();
                return;
            }

            using var stream = File.OpenRead(pngFile);
            await ctx.Channel.SendFileAsync(stream, "familytree.png", $"{user.Mention}'s Family Tree");
        }
        catch (Exception ex)
        {
            await Response().Error($"Failed to generate family tree: {ex.Message}").SendAsync();
        }
        finally
        {
            try { File.Delete(dotFile); } catch { }
            try { File.Delete(pngFile); } catch { }
            try
            {
                if (Directory.Exists(avatarDir))
                    Directory.Delete(avatarDir, true);
            }
            catch { }
        }
    }

    private static string GenerateFamilyTreeDot(ulong userId, ulong? spouseId, AdoptionInfo? adoption,
        List<ulong> children, Dictionary<ulong, string> names, Dictionary<ulong, string> avatarPaths, string avatarDir)
    {
        var roleYou = "You";
        var roleSpouse = "Spouse";
        var roleParent = "Parent";
        var roleChild = "Child";
        var sb = new StringBuilder();
        sb.AppendLine("digraph FamilyTree {");
        sb.AppendLine("    rankdir=TB;");
        sb.AppendLine("    dpi=150;");
        sb.AppendLine("    nodesep=0.5;");
        sb.AppendLine("    ranksep=1.5;");
        sb.AppendLine("    node [shape=none, width=2.0, imagescale=width, labelloc=b, margin=0.15];");
        sb.AppendLine("    edge [color=\"#99AAB5\", penwidth=2];");
        sb.AppendLine("    bgcolor=\"#2C2F33\";");
        sb.AppendLine("    fontcolor=\"white\";");
        sb.AppendLine("    margin=\"0.2\";");
        sb.AppendLine($"    imagepath=\"{avatarDir}\";");

        // User node
        sb.AppendLine($"    \"{userId}\" {BuildNode(userId, names, avatarPaths, roleYou)};");

        // Spouse
        if (spouseId.HasValue)
        {
            sb.AppendLine($"    \"{spouseId.Value}\" {BuildNode(spouseId.Value, names, avatarPaths, roleSpouse)};");
            sb.AppendLine($"    \"{userId}\" -> \"{spouseId.Value}\" [dir=none, color=\"#FF6B6B\", penwidth=3, label=\"♥\"];");
        }

        // Parents
        if (adoption != null && adoption.Parent1Id != 0)
        {
            sb.AppendLine($"    \"{adoption.Parent1Id}\" {BuildNode(adoption.Parent1Id, names, avatarPaths, roleParent)};");
            if (adoption.Parent2Id != 0)
                sb.AppendLine($"    \"{adoption.Parent2Id}\" {BuildNode(adoption.Parent2Id, names, avatarPaths, roleParent)};");
        }

        // Children
        foreach (var childId in children)
        {
            sb.AppendLine($"    \"{childId}\" {BuildNode(childId, names, avatarPaths, roleChild)};");
        }

        // Invisible node for parent connection
        if (adoption != null && adoption.Parent1Id != 0 && (children.Any() || spouseId.HasValue))
        {
            sb.AppendLine($"    \"p_{userId}\" [shape=point, width=0.01, style=invis];");
        }

        // Parent connections via invisible node
        if (adoption != null && adoption.Parent1Id != 0)
        {
            sb.AppendLine($"    \"{adoption.Parent1Id}\" -> \"p_{userId}\" [style=invis];");
            if (adoption.Parent2Id != 0)
                sb.AppendLine($"    \"{adoption.Parent2Id}\" -> \"p_{userId}\" [style=invis];");
            sb.AppendLine($"    \"p_{userId}\" -> \"{userId}\" [style=invis];");
        }

        // Invisible node for child connection
        if (children.Any())
        {
            sb.AppendLine($"    \"c_{userId}\" [shape=point, width=0.01, style=invis];");
            if (spouseId.HasValue)
            {
                sb.AppendLine($"    \"{userId}\" -> \"c_{userId}\" [style=invis];");
                sb.AppendLine($"    \"{spouseId.Value}\" -> \"c_{userId}\" [style=invis];");
            }
            else
            {
                sb.AppendLine($"    \"{userId}\" -> \"c_{userId}\" [style=invis];");
            }
            foreach (var childId in children)
            {
                sb.AppendLine($"    \"c_{userId}\" -> \"{childId}\" [style=invis];");
            }
        }

        // Ranks for proper layout
        if (adoption != null && adoption.Parent1Id != 0)
        {
            var parents = new List<string> { $"\"{adoption.Parent1Id}\"" };
            if (adoption.Parent2Id != 0) parents.Add($"\"{adoption.Parent2Id}\"");
            sb.AppendLine($"    {{ rank=same; {string.Join("; ", parents)} }}");
        }

        if (spouseId.HasValue)
        {
            sb.AppendLine($"    {{ rank=same; \"{userId}\"; \"{spouseId.Value}\" }}");
        }

        if (children.Any())
        {
            sb.AppendLine($"    {{ rank=same; {string.Join("; ", children.Select(c => $"\"{c}\""))} }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildNode(ulong userId, Dictionary<ulong, string> names,
        Dictionary<ulong, string> avatarPaths, string role)
    {
        var escapedName = EscapeHtml(names[userId]);
        var labelHtml = $"<TABLE BORDER=\"0\" CELLBORDER=\"0\" CELLPADDING=\"3\" BGCOLOR=\"#1A1D21\"><TR><TD ALIGN=\"CENTER\"><FONT FACE=\"Arial\" COLOR=\"white\" POINT-SIZE=\"14\"><B>{escapedName}</B><BR/><B>({role})</B></FONT></TD></TR></TABLE>";
        if (avatarPaths.TryGetValue(userId, out var avatarPath))
        {
            var filename = Path.GetFileName(avatarPath);
            return $"[image=\"{filename}\", label=<{labelHtml}>, width=2.0, imagescale=width, labelloc=b]";
        }
        else
        {
            return $"[label=<{labelHtml}>, width=2.0, labelloc=b]";
        }
    }

    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
